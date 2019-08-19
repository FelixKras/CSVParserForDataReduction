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
                if (new FileInfo(fpath).Extension.Contains(".json"))
                {
                    ParseAndReformatJSON(fpath);
                }
                else if (new FileInfo(fpath).Extension.Contains(".csv"))
                {
                    ParseAndReformatCSV(fpath);
                }
            }
        }

        private void ParseAndReformatCSV(string csvpath)
        {
            string[] allLines = File.ReadAllLines(csvpath);
            string[] Headers = allLines[0].Split(new char[] { ',' });
            List<FinalData> lstFinalData = new List<FinalData>();
            List<List<string>> lstParsedLines = new List<List<string>>();
            for (int ii = 1; ii < allLines.Length; ii++)
            {
                bool bRes = true;
                FinalData fd = new FinalData();
                string sInputStr = allLines[ii];
                lstParsedLines.Add(new List<string>());
                string[] s1splitted = sInputStr.Split(',');

                if (s1splitted.Length == Headers.Length)
                {
                    for (int jj = 0; jj < s1splitted.Length; jj++)
                    {
                        lstParsedLines[ii - 1].Add(s1splitted[jj]);
                    }
                }
                else if (s1splitted.Length > Headers.Length)
                {
                    for (int jj = 0; jj < s1splitted.Length; jj++)
                    {
                        if (s1splitted[jj].StartsWith("\"") && !s1splitted[jj].EndsWith("\""))
                        {
                            for (int kk = jj + 1; kk < s1splitted.Length; kk++)
                            {
                                if (s1splitted[kk].EndsWith("\""))
                                {
                                    lstParsedLines[ii - 1].Add(s1splitted.MergeParts(jj, kk));
                                    jj = kk;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            lstParsedLines[ii - 1].Add(s1splitted[jj]);
                        }
                    }
                }
                else
                {

                }

                bRes &= ParseAndCleanDate(lstParsedLines[ii - 1][0], fd);
                bRes &= ParseAndCleanTime(lstParsedLines[ii - 1][1], fd);
                fd.Location = lstParsedLines[ii - 1][2];
                fd.Operator = lstParsedLines[ii - 1][3];
                fd.Flight_no = lstParsedLines[ii - 1][4];
                fd.Route = lstParsedLines[ii - 1][5];
                fd.AC_type = lstParsedLines[ii - 1][6];
                fd.Registration = lstParsedLines[ii - 1][7];
                fd.cn_Ln = lstParsedLines[ii - 1][8];
                fd.Summary = lstParsedLines[ii - 1][12];
                ParseFatalities(fd, lstParsedLines[ii - 1][10], lstParsedLines[ii - 1][11]);
                ParseAboard(fd, lstParsedLines[ii - 1][9]);
                lstFinalData.Add(fd);
            }

            File.WriteAllText(new FileInfo(fpath).DirectoryName + "\\NewCSV.csv", lstFinalData.ConvertToString());
        }

        private bool ParseAndCleanTime(string sInputStr, FinalData fd)
        {
            DateTime dtTime = new DateTime();
            bool bRes = false;
            //Regex rgxTime = new Regex("^\\d{2}:\\d{2}\\w?|(?<=c\\s)\\d{2}:\\d{2}|^[\\?]|^\\d{4}");
            //Match timematch = rgxTime.Match(sInputStr);
            string result = String.Empty;

            result = sInputStr.Replace("c", "");
            result = result.Replace("Z", "");
            result = result.Replace(" ", "");
            result = result.Replace(".", "");
            result = result.Replace(";", "");
            if (result.Contains("?"))
            {
                result = "00:01:11";
                bRes = DateTime.TryParseExact(result, "HH:mm:ss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out dtTime);
            }
            else if (!result.Contains(":"))
            {
                result = result.Insert(2, ":");
                bRes = DateTime.TryParseExact(result, "HH:mm",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out dtTime);
            }
            else
            {
                bRes = DateTime.TryParseExact(result, "HH:mm",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out dtTime);
            }
            if (bRes)
            {
                fd.DateAndTime += new TimeSpan(dtTime.Hour, dtTime.Minute, 0);
            }
            else
            {

            }




            return bRes;
        }
        private bool ParseAndCleanDate(string sInputStr, FinalData fd)
        {
            //Regex rgxDate = new Regex("\".*?\"");
            //Match datematch = rgxDate.Match(sInputStr);
            DateTime dtDate = new DateTime();
            bool bRes = DateTime.TryParseExact(sInputStr.Replace("\"", ""), "MMMM dd yyyy",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out dtDate);
            if (bRes)
            {
                fd.DateAndTime = dtDate;
            }
            else
            {
                bRes = false;
            }

            return bRes;
        }



        private bool ParseAndCleanFlightNum(string sInputStr, FinalData fd)
        {
            bool bRes = true;
            Regex rgxFlightNum = new Regex("^\\w.*?|\\?(?=,)");
            Match FlightNummatch = rgxFlightNum.Match(sInputStr);

            if (FlightNummatch.Value.Length > 0)
            {
                if (!FlightNummatch.Value.Contains("?"))
                {
                    fd.Operator = FlightNummatch.Value;
                }
                else
                {
                    fd.Operator = "NA";
                }
                sInputStr = sInputStr.Substring(FlightNummatch.Index + FlightNummatch.Length + 1);
            }
            else
            {
                bRes = false;
            }

            return false;
        }
        private bool ParseAndCleanOperator(string sInputStr, FinalData fd)
        {
            bool bRes = true;
            Regex rgxOperNoQuotes = new Regex("^.*?(?=,)");
            Regex rgxOperQuotes = new Regex("(?<=^\").*?(?=\")");

            Match opermatchNoQuotes = rgxOperNoQuotes.Match(sInputStr);
            Match opermatchQuotes = rgxOperQuotes.Match(sInputStr);
            if (opermatchQuotes.Length > opermatchNoQuotes.Length && opermatchQuotes.Length > 0)
            {
                if (!opermatchQuotes.Value.Contains("?"))
                {
                    fd.Operator = opermatchQuotes.Value;
                }
                else
                {
                    fd.Operator = "NA";
                }
                sInputStr = sInputStr.Substring(opermatchQuotes.Index + opermatchQuotes.Length + 2);
            }
            else if (opermatchNoQuotes.Length > opermatchQuotes.Length && opermatchNoQuotes.Length > 0)
            {
                if (!opermatchNoQuotes.Value.Contains("?"))
                {
                    fd.Operator = opermatchNoQuotes.Value;
                }
                else
                {
                    fd.Operator = "NA";
                }
                sInputStr = sInputStr.Substring(opermatchNoQuotes.Index + opermatchNoQuotes.Length + 1);
            }
            else
            {
                bRes = false;
            }


            return bRes;
        }
        private bool ParseAndCleanPlace(string sInputStr, FinalData fd)
        {
            bool bRes = true;
            Regex rgxPlaceQuotes = new Regex("(?<=^\").*?(?=\",)");
            Regex rgxPlaceNoQuotes = new Regex("^\\w.+?(?=,)");
            Regex rgxQuestion = new Regex("^\\?,");
            Match placematch = rgxPlaceQuotes.Match(sInputStr);

            if (placematch.Value.Length > 0)
            {
                fd.Location = placematch.Value;
                sInputStr = sInputStr.Substring(placematch.Index + placematch.Length + 2);
            }
            else if ((placematch = rgxPlaceNoQuotes.Match(sInputStr)).Value.Length > 0)
            {
                fd.Location = placematch.Value;
                sInputStr = sInputStr.Substring(placematch.Index + placematch.Length + 1);

            }
            else if ((placematch = rgxQuestion.Match(sInputStr)).Value.Length > 0)
            {
                fd.Location = "NA";
                sInputStr = sInputStr.Substring(placematch.Index + placematch.Length);
            }
            else
            {

                bRes = false;
            }
            return bRes;
        }



        private void ParseAndReformatJSON(string jsnpath)
        {

            string allLines = File.ReadAllText(jsnpath);
            List<DataHolder> lstDataJson = JsonConvert.DeserializeObject<List<DataHolder>>(allLines);
            List<FinalData> lstFinalData = new List<FinalData>();
            for (int ii = 0; ii < lstDataJson.Count; ii++)
            {
                FinalData fd = new FinalData();
                fd.DateAndTime = ParseDate(lstDataJson[ii].Date, lstDataJson[ii].Time);
                try
                {
                    ParseFatalities(fd, lstDataJson[ii].Fatalities, lstDataJson[ii].Ground);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }

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

            if (matchCrew.Count == 1 && matchCrew[0].Length > 0)
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


            if (matchCrew.Length > 0)
            {
                fd.Crew = int.Parse(matchCrew.Value);
            }
            if (matchPsngr.Length > 0)
            {
                fd.Passengers = int.Parse(matchPsngr.Value);
            }
            if (matchTotal.Length > 0)
            {
                fd.TotalAboard = int.Parse(matchTotal.Value);
            }

            if (fd.Passengers + fd.TotalAboard != fd.TotalAboard)
            {
                fd.TotalAboard =
                    Math.Max(fd.Passengers + fd.Crew, fd.TotalAboard);
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
        public int TotalAboard;
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
                     TotalAboard + delim +
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
            sb.Append("Date,Time,Location,Operator,FlightNum,Route,AC_Type,Registration,Cn_Ln,Passengers,Crew,TotalAboard" +
                      ",Crew Fatalities,Passenger Fatalities,Total Fatalities,Ground Fatalities,Summary\r\n");
            for (int ii = 0; ii < lstData.Count; ii++)
            {
               sb.Append(lstData[ii].ToString());
            }

            return sb.ToString();
        }

        public static string MergeParts(this string[] inputStr, int Start, int End)
        {
            string result = String.Empty;

            for (int ii = Start; ii <= End; ii++)
            {
                if (ii>Start)
                {
                    if (!inputStr[ii].StartsWith(" "))
                    {
                        result += " " +inputStr[ii];
                    }
                    else
                    {
                        result += inputStr[ii];
                    }
                }
                else
                {
                    result += inputStr[ii];
                }
                

            }
            return result;
        }
    }
}
