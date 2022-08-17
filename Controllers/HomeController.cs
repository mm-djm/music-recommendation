using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using music_recommdation.Models;
using System.Data;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Net;
using Newtonsoft.Json;
using System.Text; 

namespace music_recommdation.Controllers;

public class HomeController : Controller
{
    public static int[] A = new int[5];
    public ActionResult Index()
    {
        // read and inital the training data
        ReadTrainingData();
        return View();
    }

    public ActionResult Survey()
    {
        GenerateRandom();
        return View();
    }

    [HttpPost]
    public ActionResult Recommendation(IFormCollection form)
    {
        string[] L=new string[5];
        L[0] = form["Q1"];
        L[1] = form["Q2"];
        L[2] = form["Q3"];
        L[3] = form["Q4"];
        L[4] = form["Q5"];

        IList<ArtistModel> answerList = new List<ArtistModel> {
            new ArtistModel() { ArtistID = A[0], Liked=L[0]},
            new ArtistModel() { ArtistID = A[1], Liked=L[1]},
            new ArtistModel() { ArtistID = A[2], Liked=L[2]},
            new ArtistModel() { ArtistID = A[3], Liked=L[3]},
            new ArtistModel() { ArtistID = A[4], Liked=L[4]},
        };

        var recommendations = FindRecommendation(answerList);

        SendRecommendation(recommendations);

        ViewBag.ids = recommendations;
        
        return View();
    }

    public void SendRecommendation(List<int> recommendations)
    {
        var request = (HttpWebRequest)WebRequest.Create("https://f1func-001.azurewebsites.net/api/TrainingDataUpdate?code=2EiVjdBauREP4kyVOXLUDLYCJRjJ1Ud/e6LLqL8YFBg0JP9kTU4XTw==");
        RecommendationModel recommend = new RecommendationModel() { ArtistIDs = recommendations};
        var postData = JsonConvert.SerializeObject(recommend);
        request.Method = "POST";
        request.ContentType = "application/json";
        Stream reqStream = request.GetRequestStream();
        byte[] reqBytes = Encoding.UTF8.GetBytes(postData);
        reqStream.Write(reqBytes, 0, reqBytes.Length);
        reqStream.Close();

        Console.WriteLine(postData);

        var response = (HttpWebResponse)request.GetResponse();

        var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
        Console.WriteLine(responseString);
    }

    public List<int> FindRecommendation(IList<ArtistModel> answerList)
    {
        var result = new List<int>();
        Dictionary<int,int> likeMap = new Dictionary<int,int>();
        Dictionary<int,int> dislikeMap = new Dictionary<int,int>();
        int likePersonId=0;
        int dislikePersonId=0;
        int likeMax=0;
        int dislikeMax=0;
        foreach (var answer in answerList)
        {
            // find the users interest in the same artist, then find the user with the most and least similar interests
            if (answer.Liked=="Y"){
                var users = artistMap[answer.ArtistID];
                foreach (var user in users)
                {
                    if (!likeMap.ContainsKey(user))
                    {
                        likeMap[user]=0;
                    }
                    likeMap[user]=likeMap[user]+1;
                    if (likeMax <= likeMap[user])
                    {
                        likeMax = likeMap[user];
                        likePersonId=user;
                    }
                }
            }else{
                var users = artistMap[answer.ArtistID];
                foreach (var user in users)
                {
                    if (!dislikeMap.ContainsKey(user))
                    {
                        dislikeMap[user]=0;
                    }
                    dislikeMap[user]=dislikeMap[user]+1;
                    if (dislikeMax <= dislikeMap[user])
                    {
                        dislikeMax = dislikeMap[user];
                        dislikePersonId=user;
                    }
                }
            }
        }

        // make sure not mentioned before
        Dictionary<int,int> surveyArtistMap = new Dictionary<int,int>();

        for (int i = 0; i < 5; i++)
        {
            surveyArtistMap[A[i]] = 0;
        }

        // All like or dislike
        if (dislikePersonId == 0)
        {
            while (result.Count!=5) {
                Random random = new Random();
                var num = random.Next(1,25);
                Console.WriteLine(num);
                if (!surveyArtistMap.ContainsKey(num))
                {
                    result.Add(num);
                }
            }
            return result;
        }
        if (likePersonId==0)
        {
            while (result.Count!=5) {
                Random random = new Random();
                var num = random.Next(1,25);
                Console.WriteLine(num);
                if (!surveyArtistMap.ContainsKey(num))
                {
                    result.Add(num);
                }
            }
            return result;
        }
        // Compare the user with the most and least similar interests
        var similar = userMap[likePersonId];
        var notSimilar = userMap[dislikePersonId];
        
        foreach (var item in similar)
        {
            if (result.Count==5)
            {
                break;
            }
            var key = item.Key;
            if (!notSimilar.ContainsKey(key))
            {
                if (!surveyArtistMap.ContainsKey(key))
                {
                    result.Add(key);
                }
            }
        }


        foreach (var item in likeMap)
        {
            if (result.Count==5)
            {
                break;
            }
            var u = item.Key;
            var secondSimilar = userMap[u];
            foreach (var i in secondSimilar)
            {
                if (result.Count==5)
                {
                break;
                }
                var key = i.Key;
                if (!notSimilar.ContainsKey(key))
                {
                if (!surveyArtistMap.ContainsKey(key))
                {
                    result.Add(key);
                }
            }
            }
        }
        return result;
    }

    public void GenerateRandom()
    {
        Random random = new Random();
        A[0] = random.Next(1,25);
        A[1] = random.Next(1,25);
        A[2] = random.Next(1,25);
        A[3] = random.Next(1,25);
        A[4] = random.Next(1,25);
        ViewBag.artist1 = A[0];
        ViewBag.artist2 = A[1];
        ViewBag.artist3 = A[2];
        ViewBag.artist4 = A[3];
        ViewBag.artist5 = A[4];
    }

    // Find the people who are interested in the same artist
    public static Dictionary<int,List<int>> artistMap = new Dictionary<int, List<int>>();
    // Find the artists who are interested by the same person, Liked choose Y.
    public static Dictionary<int,Dictionary<int,int>> userMap = new Dictionary<int, Dictionary<int,int>>();

    public void ReadTrainingData()
    {
        // the training data is on 2022/08/18
        using (var reader = new StreamReader("assets/TrainingData.csv"))
        {
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<ArtistModel>();

                foreach (Object obj in records)
                {
                    if (obj is ArtistModel)
                    {
                    ArtistModel a=(ArtistModel)obj;

                    if (a.Liked=="Y"){
                        if (!artistMap.ContainsKey(a.ArtistID))
                        {
                            artistMap[a.ArtistID]=new List<int>();
                        }
                        artistMap[a.ArtistID].Add(a.UserID);

                        if (!userMap.ContainsKey(a.UserID))
                        {
                            userMap[a.UserID]=new Dictionary<int,int>();
                        }
                        userMap[a.UserID][a.ArtistID]=0;
                    }

                    
                    }
                    if (obj is int)
                    {
                    Console.WriteLine("INT:{0}",obj);
                    };
                }
                Console.WriteLine();
            }
        }
    }

}
