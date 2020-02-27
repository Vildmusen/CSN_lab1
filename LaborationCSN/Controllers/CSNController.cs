using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Xml.Linq;

namespace LaborationCSN.Controllers
{
    public class CSNController : Controller
    {
        SQLiteConnection sqlite;

        public CSNController()
        {
            string path = HostingEnvironment.MapPath("/db/");
            sqlite = new SQLiteConnection($@"DataSource={path}\csn.sqlite");

        }
        XElement SQLResult(string query, string root, string nodeName)
        {
            sqlite.Open();

            var adapt = new SQLiteDataAdapter(query, sqlite);
            var ds = new DataSet(root);
            adapt.Fill(ds, nodeName);
            XElement xe = XElement.Parse(ds.GetXml());

            sqlite.Close();
            return xe;
        }


        //
        // GET: /Csn/Test
        // 
        // Testmetod som visar på hur ni kan arbeta från SQL till XML till
        // presentations-xml som sedan används i vyn.
        // Lite överkomplicerat för just detta enkla fall men visar på idén.
        public ActionResult Test()
        {
            string query = @"SELECT a.Arendenummer, s.Beskrivning, SUM(((Sluttid-starttid +1) * b.Belopp)) as Summa
                            FROM Arende a, Belopp b, BeviljadTid bt, BeviljadTid_Belopp btb, Stodform s, Beloppstyp blt
                            WHERE a.Arendenummer = bt.Arendenummer AND s.Stodformskod = a.Stodformskod
                            AND btb.BeloppID = b.BeloppID AND btb.BeviljadTidID = bt.BeviljadTidID AND b.Beloppstypkod = blt.Beloppstypkod AND b.BeloppID LIKE '%2009'
							Group by a.Arendenummer
							Order by a.Arendenummer ASC";
            XElement test = SQLResult(query, "BeviljadeTider2009", "BeviljadTid");
            XElement summa = new XElement("Total",
                (from b in test.Descendants("Summa")
                 select (int)b).Sum());
            test.Add(summa);

            // skicka presentations xml:n till vyn /Views/Csn/Test,
            // i vyn kommer vi åt den genom variabeln "Model"
            return View(test);
        }

        //
        // GET: /Csn/Index

        public ActionResult Index()
        {
            return View();
        }


        /// <summary>
        /// Runs 2 queries. 1 gets all payouts per instance and 1 groups the total sums depending on "Status". Merged into 1 xml-object and sent to uppgift1.cshtml. 
        /// Checks how many tables are needed to display all arenden and adds it to the end of the xml-object.
        /// </summary>
        /// <returns></returns>
        public ActionResult Uppgift1()
        {
            string query1 = @"SELECT a.Arendenummer AS Arendenummer, UtBetDatum, UtbetStatus, sum(bp.Belopp * ((utb.Sluttid - utb.Starttid) + 1)) AS Summa FROM Arende a
                            JOIN Utbetalningsplan u ON a.Arendenummer = u.Arendenummer 
                            JOIN Utbetalning ut ON u.UtbetPlanID = ut.UtbetPlanID
                            JOIN UtbetaldTid utb ON ut.UtbetID = utb.UtbetID
                            JOIN UtbetaldTid_Belopp ub ON utb.UtbetTidID = ub.UtbetaldTidID
                            JOIN Belopp bp ON ub.BeloppID = bp.BeloppID
                            GROUP BY a.Arendenummer, UtBetDatum";

            XElement payouts = SQLResult(query1, "Utbetalningar2009", "Utbetalning");

            string query2 = @"SELECT a.Arendenummer AS Arendenummer, UtBetDatum, UtbetStatus, sum(bp.Belopp * ((utb.Sluttid - utb.Starttid) + 1)) AS Summa, Beskrivning FROM Arende a
                            JOIN Utbetalningsplan u ON a.Arendenummer = u.Arendenummer 
                            JOIN Stodform s ON a.Stodformskod = s.Stodformskod 
                            JOIN Utbetalning ut ON u.UtbetPlanID = ut.UtbetPlanID
                            JOIN UtbetaldTid utb ON ut.UtbetID = utb.UtbetID
                            JOIN UtbetaldTid_Belopp ub ON utb.UtbetTidID = ub.UtbetaldTidID
                            JOIN Belopp bp ON ub.BeloppID = bp.BeloppID
                            GROUP BY a.Arendenummer, UtbetStatus";

            XElement payoutsTotals = SQLResult(query2, "UtbetalningsTotaler2009", "UtbetalningsTotal");

            payouts.Add(payoutsTotals);
            payouts.Add(new XElement("NrOfTables", payouts.Descendants("Utbetalning").GroupBy(x => x.Element("Arendenummer").Value).Count()));

            return View(payouts);
        }


        //
        // GET: /Csn/Uppgift2

        public ActionResult Uppgift2()
        {
            return View();
        }

        /// <summary>
        /// Runs a query to group all approved peroids sums. Sent as an xml-object to uppgift3.cshtml.
        /// </summary>
        /// <returns></returns>
        public ActionResult Uppgift3()
        {
            string query = @"SELECT Starttid AS Startdatum, Sluttid AS Slutdatum, Beskrivning AS Typ, SUM(Belopp * ((Sluttid - Starttid) + 1)) AS Summa FROM BeviljadTid b
                            JOIN Arende a ON b.Arendenummer = a.Arendenummer
                            JOIN Stodform s ON a.Stodformskod = s.Stodformskod
                            JOIN BeviljadTid_Belopp bb ON b.BeviljadTidID = bb.BeviljadTidID
                            JOIN Belopp bp ON bb.BeloppID = bp.BeloppID
                            GROUP BY a.Arendenummer, Starttid";
            XElement approvedPeriods = SQLResult(query, "BeviljadeTiderPerioder", "BeviljadTidsPeriod");
            
            return View(approvedPeriods);
        }
    }
}