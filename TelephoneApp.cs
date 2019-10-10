using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace AzureDockerApp
{
    public class Call
    {
        public int Hours { get; set; }
        public int Minutes { get; set; }
        public int Seconds { get; set; }
        public int Duration { get; set; }
        public String Telephone { get; set; }

        public Call(int x, int y, int z, String t)
        {
            this.Hours = x;
            this.Minutes = y;
            this.Seconds = z;
            this.Duration = z + 60 * y + 60 * 60 * x;
            this.Telephone = t;
        }

        public String getCallString(int callnumber)
        {
            return "Call #" + callnumber + " incoming from line " + Telephone + " with a duration of " + Duration + " seconds.\n";
        }
    }

    public class TelephoneEntry
    {
        public String Telephone { get; set; }
        public int Duration { get; set; }
        public int NumberOfCalls { get; set; }

        public TelephoneEntry(String x, int y)
        {
            this.Telephone = x;
            this.Duration = y;
            this.NumberOfCalls = 1;
        }

        public String getTelephoneString()
        {
            return "Telephone line " + Telephone + " has called " + NumberOfCalls + " times and the total duration is " + Duration + " seconds.\n";
        }

        public void AddCall(int x)
        {
            int y = this.Duration;
            this.Duration = y + x;
            NumberOfCalls++;
        }
    }

    public static class CallList
    {
        //in this list I will put all the calls
        private static List<Call> MyCalls { get; set; }
        private static List<TelephoneEntry> MyLines { get; set; }
        private static int CounterCalls { get; set; }
        public static string MessageCalls { get; set; }
        public static string MessageLines { get; set; }

        static CallList(){
            CounterCalls=0;
            MessageCalls="New call incoming! Here's the list of calls we have received so far:\n\n";
            MessageLines="\n\n\n\n\n\nHere's the list of telephone lines we have talked to so far:\n\n";

            MyCalls = new List<Call>();
            MyLines = new List<TelephoneEntry>();
        }

        public static void AddCall(int hours, int minutes, int seconds, String line)
        {
            CounterCalls++;

            MyCalls.Add(new Call(hours, minutes, seconds, line));

            MessageCalls += MyCalls[MyCalls.Count - 1].getCallString(CounterCalls);

            int index = MyLines.FindIndex(item => item.Telephone == line);
            if (index == -1)
            {
                MyLines.Add(new TelephoneEntry(line, MyCalls[MyCalls.Count - 1].Duration));
            }
            else
            {
                MyLines[index].AddCall(MyCalls[MyCalls.Count - 1].Duration);
            }
        }

        public static void ResetLinesMessage()
        {
            MessageLines = "\n\nHere's the list of telephone lines we have talked to so far:\n\n";
            for (int i = 0; i < MyLines.Count; i++)
            {
                MessageLines += MyLines[i].getTelephoneString();
            }
        }
    }


    public static class TelephoneApp
    {
        [FunctionName("TelephoneApp")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            //read call information
            string line = req.Query["line"];
            string duration = req.Query["duration"];

            bool isValid = (line != null) && (duration != null) ? true : false;

            //if call information is valid, add call to list
            if (isValid == true)
            {
                string[] unfilteredtimes = duration.Split(":");
                if (unfilteredtimes.Length == 3)
                {
                    CallList.AddCall(Int32.Parse(unfilteredtimes[0]), Int32.Parse(unfilteredtimes[1]), Int32.Parse(unfilteredtimes[2]), line);
                }
                else { 
                    isValid=false;
                }
            }

            //now update all the registered telephones
            CallList.ResetLinesMessage();

            return isValid==true
                ? (ActionResult)new OkObjectResult(CallList.MessageCalls + CallList.MessageLines)
                : new BadRequestObjectResult("Please pass a call with the correct parameters on the query string! \n\n\n" + CallList.MessageCalls + CallList.MessageLines);
        }
    }
}
