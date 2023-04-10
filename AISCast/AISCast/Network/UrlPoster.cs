using AISCast.Model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace AISCast.Network
{
    public static class UrlPoster
    {
        public static string BaseURL = null;
        private const string TimeParameter = @"time={0}";
        private const string SensorParameter = @"&l{0}={1}|{2}|{3}";

        public static bool PostData(DateTime dateTime, List<Sensor> sensors)
        {
            var url = ConstructURL(dateTime, sensors);

            return DoGetRequest(url);
        }

        private static string ConstructURL(DateTime dateTime, List<Sensor> sensors)
        {
            var urlBuilder = new StringBuilder();

            urlBuilder.Append(BaseURL);

            var timeString = string.Format(TimeParameter, dateTime.ToString("M/d/yyyyThh:mm:ss"));

            urlBuilder.Append(timeString);

            int sensorOffset = 1;
            foreach (var sensor in sensors)
            {
                urlBuilder.Append(GetSensorString(sensorOffset++, sensor));
            }

            return urlBuilder.ToString();
        }

        private static string GetSensorString(int offset, Sensor sensor)
        {
            return string.Format(SensorParameter,
                offset,
                sensor.Name,
                sensor.Status,
                sensor.Tracks);
        }

        private static bool DoGetRequest(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            var response = (HttpWebResponse)request.GetResponse();
            return response.StatusCode == HttpStatusCode.OK;
        }
    }

}
