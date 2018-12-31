using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using System.Threading;
using System.Xml;

namespace winFormArduino2web
{
    public struct sensorDataField
    {
        public string fieldName;
        public double fieldValue;
    }
    public partial class Form1 : Form
    {
        bool startStop = true;
        bool sendTestString = false;
        string testString = "<tblData><field1>13</field1><field2>26</field2><field3>30</field3></tblData>";
        static SerialPort _serialPort;
        StreamWriter sw;
        string portName;
        XmlDocument xmlDoc; XmlNode rootNode;
        int numOfFields = 1;
        sensorDataField[] FieldsArray;
        string tag1 = "<tblData>";
        string tag2 = "</tblData>";
        string tag3 = "field";
        string tag4 = "field";
        string outFileName="../../data_from_arduino.txt";
        


        public Form1()
        {
            InitializeComponent();
            listBox1.SelectedIndex = 2;
            xmlDoc = new XmlDocument();        // Create the XmlDocument.
            rootNode = xmlDoc.CreateElement("data");
            xmlDoc.AppendChild(rootNode);
        }

        private void butt_Start_Click(object sender, EventArgs e)
        {
            if (startStop)//start was clicked
            {
                _serialPort = new SerialPort();
                _serialPort.PortName = portName;// "COM3";//Set your board COM
                _serialPort.BaudRate = 9600;
                try
                {
                    _serialPort.Open();
                    butt_Start.Text = "Stop";
                    startStop = !startStop;
                    bool append = true;
                    sw = new StreamWriter("../../data_from_arduino.txt", append);
                    timer1.Interval = 200;
                    timer1.Enabled = true;
                    label_messages.Text = "";
                }
                catch (Exception ex)
                {
                    // Handle exception
                    label_messages.Text = ex.Message;
                    _serialPort.Close();

                }
               
            }
            else //Stop was clicked
            {
                butt_Start.Text = "Start";
                startStop = !startStop;
                timer1.Enabled = false;
                _serialPort.Close();
                sw.Flush();
                sw.Close();
                label_messages.Text = "";
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            string a = _serialPort.ReadExisting();
            textBox1.Text = a;
            sw.WriteLine(a);
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Get the currently selected item in the ListBox.
            portName = listBox1.SelectedItem.ToString();
            //textBox1.Text = portName;
        }

        private void butOnce_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(textBox_fields_numbers.Text,out numOfFields))
            {
                MessageBox.Show("The number of fields must be a number", "Error Detected in Input", MessageBoxButtons.OK);
                textBox_fields_numbers.Text = "1";
                return;
            }
            if (numOfFields < 1)
            {   MessageBox.Show("The number of fields must be >0", "Error Detected in Input", MessageBoxButtons.OK);
                textBox_fields_numbers.Text = "1";
                return;
            }
            FieldsArray = new sensorDataField[numOfFields];// creating array of fields names and values

            _serialPort = new SerialPort();
            _serialPort.PortName = portName;// "COM3";//Set your board COM
            _serialPort.BaudRate = 9600;
            try
            {
                string a;
                if (!sendTestString)
                {
                    _serialPort.Open();
                     a = _serialPort.ReadExisting(); // get string from COM
                }
                else
                     a = textBox1.Text;
                string pa="";
                pa = ParseXml(a); // string that starts and ends with <tbl>
                //string parsedTag = ParseXmlTag(ref a);
                //LoadXml(pa); // עדיין צריך לפתור עניין אוביקט

                textBox1.Text = pa;
                Write2File(outFileName,pa);
                _serialPort.Close();
                if (checkBox1.Checked) // sending to Thingspeak
                {
                    while (pa.Length != 0)
                    {
                        string tagString = ParseXmlTag(ref pa); // get first <tblData> xml node
                        if (!LoadXml2Array(tagString, FieldsArray)) continue; // array faild
                    
                    label_messages.Text = "Thingspeak: opening web request";
                    openWebThingspeak(FieldsArray);
                    label_messages.Text = "finished web request";
                    }
                }
                if (checkBox2.Checked) // sending to Bigbangs
                {
                    label_messages.Text = "Bigbangs: opening web request";
                    openWebBigbangs(pa);
                    label_messages.Text = "finished web request";
                }

            }
            catch (Exception ex)
            {
                // Handle exception
                label_messages.Text = ex.Message;
                _serialPort.Close();

            }

        }
        private void Write2File(string fileName,string str)
        {
            bool append = true;
            sw = new StreamWriter("../../data_from_arduino.txt", append);
            sw.WriteLine(str);
            sw.Flush();
            sw.Close();
        }
        /// <summary>
        /// input a parsed string with correct tags in the begining and end
        /// the fuction trys to load it into a sensorDataField array
        /// </summary>
        /// <param name="pa"></param>
        private bool LoadXml2Array(string tagString,sensorDataField[] tbl)
        {
                for (int i = 1; i <= numOfFields; i++)
                {
                    string openTag = "<"+tag3 + i.ToString() + ">";
                    string closeTag = "</"+ tag3 + i.ToString() + ">";
                    string fieldVal = ParseXmlTag(ref tagString, openTag, closeTag);
                    tbl[i - 1].fieldName = tag3 + i.ToString();
                    if (!double.TryParse(fieldVal,out tbl[i-1].fieldValue))
                    { return false;} //field value failed. skipping the whole <tbl> string
                    
                }
                //try
                //{
                //    xmlDoc.LoadXml(tagString);
                //}
                //catch (XmlException e)
                //{ }
            return true;
        }
        
        /// <summary>
        /// input a parsed string with correct tags in the begining and end
        /// the fuction trys to load it into an XML doc
        /// </summary>
        /// <param name="pa"></param>
        private void LoadXml(string pa)
        {
            while (pa.Length != 0)
            {
                string tagString = ParseXmlTag(ref pa);
                try
                {
                    xmlDoc.LoadXml(tagString);
                }
                catch (XmlException e)
                { }
            }
        }
        /// <summary>
        /// gets a string, parses tags and load into an XML doc type
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private XmlDocument ParseLoadXml(string str)
        {
            // Create the XmlDocument.
            XmlDocument doc = new XmlDocument();

            return doc;
        }
        /// <summary>
        /// parses and returns first tag1 to tag2 from string
        /// the original string is reducted from the parsed tag string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private string ParseXmlTag(ref string str)
        {
            string st;
            int i = str.IndexOf(tag1);
            int j = str.IndexOf(tag2);
            if (i > j) // first </> tag is before the <> tag
            {
                str = str.Substring(j + tag2.Length + 1);
                i = str.IndexOf(tag1);
                j = str.IndexOf(tag2);
            }
            if (!str.Contains(tag1) || !str.Contains(tag2))
                return (string.Empty);
            st = str.Substring(i, (j - i + tag2.Length));
            if ((j - i + tag2.Length) + 1 >= str.Length)
                str = "";
            else
                str = str.Substring((j - i + tag2.Length) + 1);
            return st;
        }
        /// <summary>
        /// parses and returns value of first tag from string
        /// the original string is reducted from the parsed tag string
        /// </summary>
        /// <param name="str"></param><param name="openTag"></param><param name="closeTag"></param>
        /// <returns>string</returns>
        private string ParseXmlTag(ref string str,string openTag,string closeTag)
        {
            string st;
            int i = str.IndexOf(openTag);
            int j = str.IndexOf(closeTag);
            if (i > j) // first </> tag is before the <> tag
            {
                str = str.Substring(j + closeTag.Length + 1); // cut the damaged string
                i = str.IndexOf(openTag); // find new indexes
                j = str.IndexOf(closeTag);
            }
            if (!str.Contains(openTag) || !str.Contains(closeTag))
                return (string.Empty);
            //st = str.Substring(i, (j - i + closeTag.Length)); // get the string with tags
            st = str.Substring(i + openTag.Length, (j - i - openTag.Length)); // get the value without tags
            str = str.Substring((j - i + closeTag.Length) + 1); // reduce the original string
            return st;
        }
        /// <summary>
        /// returns a string that begins and ends with XML tag
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private string ParseXml(string str)
        {
            string tag1="<tblData>";
            string tag2 = "</tblData>";
            string st;
            if (!str.Contains(tag1)||!str.Contains(tag2))
                return ("");
            int i=str.IndexOf(tag1);
            int j = str.LastIndexOf(tag2);
            return str.Substring(i, (j - i + tag2.Length));
        }
        private void openWebThingspeak(sensorDataField[] FieldsArray)
        {
            string WRITEKEY = textBox2.Text;
            //string WRITEKEY = "DC10XQ6LEHXOOAHZ"; // https://thingspeak.com/channels/656336/private_show
            string strUpdateBase = "http://api.thingspeak.com/update";
            string strUpdateURI = strUpdateBase + "?key=" + WRITEKEY;
            for (int i = 0; i < FieldsArray.Length; i++)
            {
                strUpdateURI += "&" + FieldsArray[i].fieldName + "=" + FieldsArray[i].fieldValue.ToString();
            }
            try
            {
                //string strField1 = "20";
                //string strField2 = "40";
                ////HttpWebRequest ThingsSpeakReq;
                ////HttpWebResponse ThingsSpeakResp;
                //strUpdateURI += "&field1=" + strField1;
                //strUpdateURI += "&field2=" + strField2;

                //                ThingsSpeakReq = (HttpWebRequest)WebRequest.Create(strUpdateURI);
                WebRequest request = WebRequest.Create(strUpdateURI);//
                WebResponse response = request.GetResponse();

                //ThingsSpeakResp = (HttpWebResponse)ThingsSpeakReq.GetResponse();

                //if (!(string.Equals(ThingsSpeakResp.StatusDescription, "OK")))
                //{
                //    Exception exData = new Exception(ThingsSpeakResp.StatusDescription);
                //    throw exData;
                //}
            }
            catch (Exception ex)
            {
                textBox3.Text = ex.Message;
                throw;
            }
        }

        private void openWebThingspeakTest()
        {
            try
            {

                string WRITEKEY = textBox2.Text;
                //string WRITEKEY = "DC10XQ6LEHXOOAHZ"; // https://thingspeak.com/channels/656336/private_show
                string strUpdateBase = "http://api.thingspeak.com/update";
                string strUpdateURI = strUpdateBase + "?key=" + WRITEKEY;
                string strField1 = "20";
                string strField2 = "40";
                //HttpWebRequest ThingsSpeakReq;
                //HttpWebResponse ThingsSpeakResp;
                strUpdateURI += "&field1=" + strField1;
                strUpdateURI += "&field2=" + strField2;

//                ThingsSpeakReq = (HttpWebRequest)WebRequest.Create(strUpdateURI);
                WebRequest request = WebRequest.Create(strUpdateURI);//
                WebResponse response = request.GetResponse();

                //ThingsSpeakResp = (HttpWebResponse)ThingsSpeakReq.GetResponse();

                //if (!(string.Equals(ThingsSpeakResp.StatusDescription, "OK")))
                //{
                //    Exception exData = new Exception(ThingsSpeakResp.StatusDescription);
                //    throw exData;
                //}
            }
            catch (Exception ex)
            {
                textBox3.Text = ex.Message;
                throw;
            }
        }
        private void openWebBigbangs(string pa)
        {
            try
            {

                string FOLDER = textBox4.Text;
                string strUpdateBase = "http://bigbangs.work/";
                string strUpdateURI = strUpdateBase + FOLDER+"/index.php/" + "?data=";
                strUpdateURI += pa;

                WebRequest request = WebRequest.Create(strUpdateURI);//
                WebResponse response = request.GetResponse();
                textBox3.Text = ((HttpWebResponse)response).StatusDescription;
                response.Close();
                //if (!(string.Equals(ThingsSpeakResp.StatusDescription, "OK")))
                //{
                //    Exception exData = new Exception(ThingsSpeakResp.StatusDescription);
                //    throw exData;
                //}
            }
            catch (Exception ex)
            {
                textBox3.Text = ex.Message;
                throw;
            }
        }

        private void button1_Click(object sender, EventArgs e) // Opens txt file location
        {
            if (File.Exists(outFileName))
            {
                string absPath = Path.GetFullPath(outFileName);
                System.Diagnostics.Process.Start("explorer.exe", " /select, " + absPath);
            }
        }

        private void checkBoxTest_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBoxTest.Checked)
            {
                checkBoxTest.Text = "Check to send test String";
                textBox1.ReadOnly = true;
                textBox1.Text = "";
                sendTestString = false;
            }
            else
            {
                checkBoxTest.Text = "Write in text box string to send";
                textBox1.ReadOnly = false;
                textBox1.Text = testString;
                sendTestString = true;
            }
        }
        

       
    }
}
