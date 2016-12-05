using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xamarin.Forms;

namespace TabTest
{
    public partial class tab1 : ContentPage
    {
        private static readonly DateTime UNIX_EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public tab1()
        {
            InitializeComponent();

            string API_KEY = "3926503eda7d17b1dd286f43414211cd";
            string List_URL = "http://api.openweathermap.org/data/2.5/forecast";
            string Today_URL = "http://api.openweathermap.org/data/2.5/weather";
            string degree = "units=metric";
            string CITY_NAME = "Hamamatsu-shi";

            //3時間ごとの天気をListで取得
            string urlList = List_URL + "/city?q=" + CITY_NAME + "&" + degree + "&APPID=" + API_KEY;
            WeatherDisplay(urlList);
            
            //現在の天気を取得
            string urlToday = Today_URL + "?q=" + CITY_NAME + "&" + degree + "&APPID=" + API_KEY;
            TodayDisplay(urlToday);

            //Pull to Refresh
            TodayList.IsPullToRefreshEnabled = true;

            TodayList.Refreshing += async (sender, e) =>
            {
                await TodayDisplay(urlToday);
                await WeatherDisplay(urlList);
                TodayList.EndRefresh();
            };

            //TodayListのボーダーを無効化
            TodayList.SeparatorVisibility = SeparatorVisibility.None;

            //TodayListとListViewを選択できないように
            TodayList.ItemSelected += (sender, e) => {
                ((ListView)sender).SelectedItem = null;
            };
            Listview.ItemSelected += (sender, e) => {
                ((ListView)sender).SelectedItem = null;
            };
        }

        public async Task<string> GetContentsAsync(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            //UIスレッドに戻さない
            var response = await request.GetResponseAsync().ConfigureAwait(false);

            StreamReader sr = new StreamReader(response.GetResponseStream());
            string str = sr.ReadToEnd();

            return str;
        }
        //UNIX時間を日本時間に
        public static DateTime FromUnixTime(long unixTime)
        {
            return UNIX_EPOCH.AddSeconds(unixTime).ToLocalTime();
        }       
        //現在の天気を表示
        public async Task TodayDisplay(string urlToday)
        {
            var strToday = await GetContentsAsync(urlToday);
            var resultToday = JsonConvert.DeserializeObject<OpenWeatherMapToday.RootObject>(strToday);
            var weatherToday = resultToday.weather[0];

            List<WeatherToday> today = new List<WeatherToday>();
            today.Add(new WeatherToday
            {
                City = resultToday.name,
                Date = FromUnixTime(resultToday.dt).ToString(),
                Weather = weatherToday.main,
                WeatherIcon = "http://openweathermap.org/img/w/" + weatherToday.icon + ".png",
                Temp = resultToday.main.temp.ToString(),
                TempMax = "↑" + resultToday.main.temp_max.ToString() + "℃",
                TempMin = "↓" + resultToday.main.temp_min.ToString() + "℃"
            });
            TodayList.ItemsSource = today;
        }
        //3時間ごとの天気を表示
        public async Task WeatherDisplay(string urlList)
        {
            var strList = await GetContentsAsync(urlList);
            var resultList = JsonConvert.DeserializeObject<OpenWeatherMapList.RootObject>(strList);

            //３時間ごとの天気をListViewに追加
            List<WeatherList> weatherLists = new List<WeatherList>();
            for (int i = 1; i <= 5; i++)
            {
                weatherLists.Add(new WeatherList
                {
                    Date = FromUnixTime(resultList.list[i].dt).ToString(),
                    Temp = resultList.list[i].main.temp.ToString() + "℃",
                    Weather = resultList.list[i].weather[0].main,
                    WeatherIcon = "http://openweathermap.org/img/w/" + resultList.list[i].weather[0].icon + ".png"
                });
            }
            Listview.ItemsSource = weatherLists;
        }
    }
}
