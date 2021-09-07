using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HttpClientDemo
{
	class Program
	{
		static async System.Threading.Tasks.Task Main(string[] args)
		{
			try
			{
				var config = SpeechTranslationConfig.FromSubscription("2ef28b464ed34f30960419794fe5c601", "eastus");

				// Sets source and target languages.
				string fromLanguage = "en-US";
				config.SpeechRecognitionLanguage = fromLanguage;

				var allowedTranslations = new List<string> { "en-US", "de-DE", "hi-HI", "bs-HR", "ro-RO", "mr-MR", "gu-GU" }; ;

				foreach (var translation in allowedTranslations)
				{
					config.AddTargetLanguage(translation.Split('-')[0]);
				}

				using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();

				using var recognizer = new TranslationRecognizer(config, audioConfig);

				//recognizer.Recognizing += Recognizer_RecognizingAsync;
				recognizer.SessionStarted += Recognizer_SessionStarted; ;

				recognizer.Canceled += Recognizer_Canceled;
				recognizer.Recognizing += async (s, e) =>
				{
					Console.WriteLine("speech is recognizing" + DateTime.Now);

					if (e.Result.Reason != ResultReason.TranslatingSpeech) return;

					var translations = new Translations
					{
						OffSet = e.Result.OffsetInTicks.ToString(),
						Languages = new Dictionary<string, string>()
					};


					foreach (var translationLangauage in allowedTranslations)
					{
						e.Result.Translations.TryGetValue(GetLanguageCode(translationLangauage), out string translation);
						translations.Languages.Add(translationLangauage.Split('-')[0], translation);
					}

					await BroadCastTranslation(translations);
					await BroadCastTranslationToLocal(translations);
					Console.WriteLine("recognizing End" + DateTime.Now);
				};

				do
				{
					await recognizer.StartContinuousRecognitionAsync();

				}
				while (Console.ReadKey(true).Key == ConsoleKey.Enter);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}

		Console.WriteLine("Done");
		}

		private static void Recognizer_Canceled(object sender, TranslationRecognitionCanceledEventArgs e)
		{
			
		}

		private static void Recognizer_SessionStarted(object sender, SessionEventArgs e)
		{
			Console.WriteLine(e.SessionId);
		}

		

		private static void Recognizer_SessionStopped(object sender, SessionEventArgs e)
		{
			Console.WriteLine(e.SessionId);
		}

		private static string GetLanguageCode(string translationLangauage)
		{
			return translationLangauage.Split('-')[0];
		}

		private static async Task BroadCastTranslationToLocal(Translations translations)
		{
			Console.WriteLine("Recognized i am sending the data " + DateTime.Now);
			var json = JsonConvert.SerializeObject(translations);
			var data = new StringContent(json, Encoding.UTF8, "application/json");

			var url = "http://localhost:7071/api/BroadcastTranslation";
			using var client = new HttpClient();

			await client.PostAsync(url, data);
		}

		private static async Task BroadCastTranslation(Translations translations)
		{
			Console.WriteLine("Recognized i am sending the data "  + DateTime.Now);
			var json = JsonConvert.SerializeObject(translations);
			var data = new StringContent(json, Encoding.UTF8, "application/json");

			var url = "https://realtimetranslator.azurewebsites.net/api/BroadcastTranslation";
			using var client = new HttpClient();

			await client.PostAsync(url, data);
		}
	}
}

public class Translations
{
	public string OffSet { get; set; }
	public Dictionary<string,string> Languages { get; set; }
}
