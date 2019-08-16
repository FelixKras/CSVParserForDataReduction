using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace ReformatDatabase
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
        string fpath;
        private void Button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "|*.csv;*.json";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                fpath = ofd.FileName;
                ParseAndReformat(fpath);
            }
        }


        private void ParseAndReformat(string fpath)
        {

            string allLines = File.ReadAllText(fpath);
            List<DataHolder> lstDataJson = JsonConvert.DeserializeObject<List<DataHolder>>(allLines);
            List<FinalData> lstFinalData = new List<FinalData>();
            for (int ii = 0; ii < lstDataJson.Count; ii++)
            {
                FinalData fd = new FinalData();
                fd.DateAndTime = ParseDate(lstDataJson[ii].Date, lstDataJson[ii].Time);
                ParseFatalities(fd, lstDataJson[ii].Fatalities, lstDataJson[ii].Ground);
                ParseAboard(fd, lstDataJson[ii].Aboard);
                

                fd.Location = lstDataJson[ii].Location;
                fd.Operator = lstDataJson[ii].Operator;
                fd.Flight_no = lstDataJson[ii].Flight_no;
                fd.Route = lstDataJson[ii].Route;
                fd.AC_type = lstDataJson[ii].AC_type;
                fd.Registration = lstDataJson[ii].Registration;
                fd.cn_Ln = lstDataJson[ii].cn_ln;
                fd.Summary = lstDataJson[ii].Summary;
                lstFinalData.Add(fd);

            }

            JsonSerializerSettings jsnSettings = new JsonSerializerSettings();
            jsnSettings.DateFormatString = "dd/MM/yyyy HH:mm";
            jsnSettings.DateFormatHandling = DateFormatHandling.MicrosoftDateFormat;
            string NewJson = JsonConvert.SerializeObject(lstFinalData, Formatting.Indented, jsnSettings);

            File.WriteAllText(new FileInfo(fpath).DirectoryName + "\\NewJson.json", NewJson);
            File.WriteAllText(new FileInfo(fpath).DirectoryName + "\\NewCSV.csv", lstFinalData.ConvertToString());
        }

        private void ParseFatalities(FinalData fd, string airFatalities, string grndFatalities)
        {
            Regex rgxCrew = new Regex(@"(?<=crew:)\d*");
            Regex rgxPsngr = new Regex(@"(?<=passengers:)\d*");
            Regex rgxTotal = new Regex(@"^\d*");


            MatchCollection matchCrew = rgxCrew.Matches(airFatalities);
            MatchCollection matchPsngr = rgxPsngr.Matches(airFatalities);
            MatchCollection matchAirTotal = rgxTotal.Matches(airFatalities);
            MatchCollection matchGrndTotal = rgxTotal.Matches(grndFatalities);

            if (matchCrew.Count == 1&& matchCrew[0].Length>0 )
            {
                fd.PlaneCrewFatalities = int.Parse(matchCrew[0].Value);
            }
            if (matchPsngr.Count == 1 && matchPsngr[0].Length > 0)
            {
                fd.PlanePsngrFatalities = int.Parse(matchPsngr[0].Value);
            }

            if (matchAirTotal.Count == 1 && matchAirTotal[0].Length > 0)
            {
                fd.PlaneTotalFatalities = int.Parse(matchAirTotal[0].Value);
            }

            if (fd.PlanePsngrFatalities + fd.PlaneCrewFatalities != fd.PlaneTotalFatalities)
            {
                fd.PlaneTotalFatalities =
                    Math.Max(fd.PlanePsngrFatalities + fd.PlaneCrewFatalities, fd.PlaneTotalFatalities);
            }

            if (matchGrndTotal.Count == 1 && matchGrndTotal[0].Length > 0)
            {
                fd.GroundFatalities = int.Parse(matchGrndTotal[0].Value);
            }
        }

        private void ParseAboard(FinalData fd, string aboard)
        {
            Regex rgxCrew = new Regex(@"(?<=crew:)\d*");
            Regex rgxPsngr = new Regex(@"(?<=passengers:)\d*");
            Regex rgxTotal = new Regex(@"^\d*");


            Match matchCrew = rgxCrew.Match(aboard);
            Match matchPsngr = rgxPsngr.Match(aboard);
            Match matchTotal = rgxTotal.Match(aboard);


            if (matchCrew.Length == 1)
            {
                fd.Crew = int.Parse(matchCrew.Value);
            }
            if (matchPsngr.Length == 1)
            {
                fd.Passengers = int.Parse(matchPsngr.Value);
            }



        }
        private DateTime ParseDate(string date, string time)
        {
            DateTime dtDate = new DateTime();
            DateTime dtTime = new DateTime();
            DateTime result = new DateTime();
            bool bRes = DateTime.TryParseExact(date, "MMMM dd, yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out dtDate);
            if (bRes && time != string.Empty)
            {
                bRes = DateTime.TryParseExact(time, "HH:mm",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out dtTime);
                if (bRes)
                {
                    result = dtDate + new TimeSpan(dtTime.Hour, dtTime.Minute, 0);
                }
                else
                {
                    result = dtDate + new TimeSpan(12, 34, 0);
                }
            }

            return result;
        }
    }

    class DataHolder
    {
        public string Date;
        public string Time;
        public string Location;
        public string Operator;
        public string Flight_no;
        public string Route;
        public string AC_type;
        public string Registration;
        public string cn_ln;
        public string Aboard;
        public string Fatalities;
        public string Ground;
        public string Summary;
    }

    class FinalData
    {
        public DateTime DateAndTime;
        public string Location;
        public string Operator;
        public string Flight_no;
        public string Route;
        public string AC_type;
        public string Registration;
        public string cn_Ln;
        public int Passengers;
        public int Crew;
        public int PlaneCrewFatalities;
        public int PlanePsngrFatalities;
        public int PlaneTotalFatalities;
        public int GroundFatalities;
        public string Summary;

        public FinalData()
        {
            //DateAndTime;
            Location = String.Empty;
            Operator = String.Empty;
            Flight_no = String.Empty;
            Route = String.Empty;
            AC_type = String.Empty;
            Registration = String.Empty;
            cn_Ln = String.Empty;
            Summary = String.Empty;
        }
        public override string ToString()
        {
            string result = String.Empty;
            string delim = ",";
            string datestr = DateAndTime.ToString("dd/MM/yyyy");
            string timestr = DateAndTime.ToString("HH:mm");
            result = datestr + delim +
                     timestr + delim +
                     Location.Replace(',', '.').Replace('\'', ' ').Replace('"', ' ') + delim +
                     Operator.Replace(',', '.').Replace('\'', ' ').Replace('"', ' ') + delim +
                     Flight_no.Replace(',', '.').Replace('\'', ' ').Replace('"', ' ') + delim +
                     Route.Replace(',', '.').Replace('\'', ' ').Replace('"', ' ') + delim +
                     AC_type.Replace(',', '.').Replace('\'', ' ').Replace('"', ' ') + delim +
                     Registration.Replace(',', '.').Replace('\'', ' ').Replace('"', ' ') + delim +
                     cn_Ln.Replace(',', '.').Replace('\'', ' ').Replace('"', ' ') + delim +
                     Passengers + delim +
                     Crew + delim +
                     PlaneCrewFatalities + delim +
                     PlanePsngrFatalities + delim +
                     PlaneTotalFatalities + delim +
                     GroundFatalities + delim +
                     Summary.Replace(',', '.').Replace('\'', ' ').Replace('"', ' ') + "\r\n";
            return result;
        }
    }

    static class ExtentionMethods
    {
        public static string ConvertToString(this List<FinalData> lstData)
        {
            StringBuilder sb = new StringBuilder(lstData.Count);
            sb.Append("Date,Time,Location,Operator,FlightNum,Route,AC_Type,Registration,Cn_Ln,Passengers,Crew," +
                      "Crew Fatalities,Passenger Fatalities,Total Fatalities,Ground Fatalities,Summary\r\n");
            for (int ii = 0; ii < lstData.Count; ii++)
            {
                if (lstData[ii].AC_type.Contains("De Havilland"))
                {

                }
                sb.Append(lstData[ii].ToString());
            }

            return sb.ToString();
        }
    }
}
