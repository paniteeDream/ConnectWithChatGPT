using CsvHelper.Configuration;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.Configuration;

public partial class _Default : System.Web.UI.Page
{
    string OpenAIApiKey = ConfigurationManager.AppSettings["OpenAIApiKey"];
    string OpenAIEndpoint = ConfigurationManager.AppSettings["OpenAIEndpoint"];

    protected async void Translate_Click(object sender, EventArgs e)
    {
        DateTime startTime = DateTime.Now;
        await TranslateAsync();
        BindGridView();
        processTime(startTime);
    }

    protected async Task TranslateAsync()
    {
        if (fileInput.PostedFile.ContentType == "text/csv")
        {
            try
            {
                var inputStream = fileInput.PostedFile.InputStream;
                var translationRecords = await TranslateCSV(inputStream);

                Session["TranslationRecords"] = translationRecords;
                resultLabel.Text = "Translation successful !" ;
            }
            catch (Exception ex)
            {
                resultLabel.Text = "Error: " + ex.Message;
            }
        }
        else
        {
            resultLabel.Text = "Please select a CSV file.";
        }
    }



    protected void BindGridView()
    {
        var translationRecords = Session["TranslationRecords"] as List<TranslationRecord>;

        if (translationRecords != null && translationRecords.Any())
        {
            resultGridView.DataSource = translationRecords;
            resultGridView.DataBind();
        }
        else
        {
            resultLabel.Text = "Please select a CSV file";
        }
    }

    protected void Download_Click(object sender, EventArgs e)
    {

        var translationRecords = Session["TranslationRecords"] as List<TranslationRecord>;


        if (translationRecords != null && translationRecords.Any())
        {
            try
            {
                string outputFileName = "Translated_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv";
                Response.Clear();
                Response.ContentType = "text/csv";
                Response.AddHeader("Content-Disposition", "attachment;filename=" + outputFileName);

                using (var writer = new StreamWriter(Response.OutputStream))
                using (var csvWriter = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    csvWriter.WriteRecords(translationRecords);
                }

                Response.Flush();
                Response.End();
            }
            catch (Exception ex)
            {
                resultLabel.Text = "Error: " + ex.Message;
            }
        }
        else
        {
            resultLabel.Text = "Please translate a CSV file before downloading.";
        }
    }

    protected async Task<List<TranslationRecord>> TranslateCSV(Stream inputStream)
    {
        List<string> listword = new List<string>();
        using (var reader = new StreamReader(inputStream))
        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
        {
            while (csv.Read())
            {
                listword.Add(csv.GetField<string>(0));
            }
        }
        return await ChunkStep(listword);
    }
    protected async Task<List<TranslationRecord>> ChunkStep(List<string> listword)
    {
        List<TranslationRecord> translationRecords = new List<TranslationRecord>();
        int totalWords = listword.Count;
        int chunkSize = 10;
        for (int i = 0; i < totalWords; i += chunkSize)
        {
            int size = Math.Min(chunkSize, totalWords - i);
            string resultString = string.Join(", ", listword.GetRange(i, size));
            Dictionary<int, List<string>> result = await Backoff(Data: resultString, chunkSize: size);

            foreach (var item in result)
            {
                translationRecords.Add(new TranslationRecord
                {
                    OriginalWords = listword[item.Key + i],
                    English = item.Value[0],
                    Thai = item.Value[1]
                });
            };
            if (i + chunkSize < totalWords)
            {
                await Delay(20000);
            }
        }
        return translationRecords;
    }
    private async Task<Dictionary<int, List<string>>> Backoff(string Data, int chunkSize)
    {
        string Result = "";
        string Prompt = string.Format(
            "{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}\n{7}",
            "- Translate words into English and Thai.",
            "- Data : ["+Data+"]",
            "- Translate to complete the number. Total data: "+chunkSize+" records",
            "- Translate every word in the list",
            "- Please answer in this format.",
            "- format : {\"0\":[\"translation Eng \", \"translation Thai \"],\"1\":[\"translation Eng \", \"translation Thai \"], \"2\":[\"translation Eng \", \"translation Thai \"],\"3\":[\"translation Eng \", \"translation Thai \"], ...}",
            "- Do not answer anything other than the translated words.",
            "- The output contains only json."
            );
        Dictionary<int, List<string>> Error = new Dictionary<int, List<string>>();
        for (int i = 1; i <= 2; i++)
        {
            try
            {
                Result = await CallOpenAITranslationApi(prompt: Prompt, lenCharacter: Data.Count());
                Dictionary<int, List<string>> DicData = JsonSerializer.Deserialize<Dictionary<int, List<string>>>(Result.Replace(".", ""));
                if (DicData.Count() == chunkSize) { return DicData; }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        };
        for (int j = 0; j < chunkSize; j++)
        {
            List<string> errorText = new List<string>();
            errorText.Add("error" + Result);
            errorText.Add("error" + Result);
            Error.Add(j, errorText);
        }
        return Error;
    }

    private async Task<string> CallOpenAITranslationApi(string prompt, int lenCharacter)
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + OpenAIApiKey);
                var requestBody = new { model= "gpt-3.5-turbo-instruct", prompt, max_tokens = 10*lenCharacter };
                var jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(OpenAIEndpoint, content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var translation = Newtonsoft.Json.JsonConvert.DeserializeObject<OpenAIResponse>(responseContent);
                    return translation.choices[0].text.Trim();
                }
                else
                {
                    return string.Format("HTTP Error: {0} - {1}", (int)response.StatusCode, response.ReasonPhrase);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Translation failed. Exception:" + ex.Message);
            return "Translation failed. Exception:" + ex.Message;
        }
    }

    private async Task Delay(int milliseconds)
    {
        await Task.Delay(milliseconds);
    }

    public class TranslationRecord
    {
        public string OriginalWords { get; set; }
        public string English { get; set; }
        public string Thai { get; set; }
    }

    private class OpenAIResponse
    {
        public Choice[] choices { get; set; }
    }

    private class Choice
    {
        public string text { get; set; }
    }

    protected void processTime(DateTime startTime)
    {
        DateTime endTime = DateTime.Now;
        TimeSpan processingTime = endTime - startTime;
        int procTime = (int)processingTime.TotalMilliseconds;
        int min = (procTime / 1000) / 60;
        int sec = (procTime / 1000) - (min * 60);
        lblProcessingTime.Text = "Processing Time: " + min.ToString() + " Minutes " + sec.ToString() + " Seconds";
        updatePanel.Update();
    }
}
