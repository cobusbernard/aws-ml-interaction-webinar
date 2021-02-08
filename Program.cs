using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.Polly;
using Amazon.Polly.Model;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.TranscribeService;
using Amazon.TranscribeService.Model;
using Amazon.Translate;
using Amazon.Translate.Model;

namespace aws_ml_interaction_webinar
{
    class Program
    {

        private async void ProcessImage() {
            var photo_filename = "dog_shoe.jpg";

            AmazonRekognitionClient rekognitionClient = new AmazonRekognitionClient();

            Amazon.Rekognition.Model.Image image = new Amazon.Rekognition.Model.Image();
            try
            {
                using (FileStream fs = new FileStream(photo_filename, FileMode.Open, FileAccess.Read))
                {
                    byte[] data = null;
                    data = new byte[fs.Length];
                    fs.Read(data, 0, (int)fs.Length);
                    image.Bytes = new MemoryStream(data);
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to load file " + photo_filename);
                return;
            }

            DetectLabelsRequest detectlabelsRequest = new DetectLabelsRequest()
            {
                Image = image,
                MaxLabels = 20,
                MinConfidence = 95F
            };

            try
            {
                var detectLabelsResponse = await rekognitionClient.DetectLabelsAsync(detectlabelsRequest);
                Console.WriteLine("Detected labels for " + photo_filename);
                foreach (Label label in detectLabelsResponse.Labels)
                    Console.WriteLine("{0}: {1}", label.Name, label.Confidence);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private async void ProcessTextToSpeech() {
            try {
                AmazonPollyClient pollyClient = new AmazonPollyClient();

                SynthesizeSpeechRequest request = new SynthesizeSpeechRequest(){
                    Text = "Hello world, how are you doing?",
                    OutputFormat = OutputFormat.Mp3,
                    VoiceId = VoiceId.Celine
                };

                var response = await pollyClient.SynthesizeSpeechAsync(request);

                using (var fileStream = File.Create(@"Celine.mp3"))
                {
                    response.AudioStream.CopyTo(fileStream);
                    fileStream.Flush();
                    fileStream.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private async void ProcessSpeechtoText() {
            try {
                AmazonTranscribeServiceClient transcribeClient =
                new AmazonTranscribeServiceClient();

                var job_name = "webinar-code";

                var request = new StartTranscriptionJobRequest() {
                    TranscriptionJobName = job_name,
                    LanguageCode = "en-GB",
                    MediaSampleRateHertz = 16000,
                    MediaFormat = "mp3",
                    Media = new Media(){
                        MediaFileUri = "s3://webinar-demo-cobus/cobus.mp3"
                    }
                };

                var response = await transcribeClient.StartTranscriptionJobAsync(request);

                while (true) {
                    var job_response = await transcribeClient.GetTranscriptionJobAsync(new GetTranscriptionJobRequest() {
                        TranscriptionJobName = job_name
                    });

                    if (job_response.TranscriptionJob.TranscriptionJobStatus == TranscriptionJobStatus.COMPLETED) {
                        Console.WriteLine("Job has completed.");

                        var processing_response = await transcribeClient.GetTranscriptionJobAsync(new GetTranscriptionJobRequest() {
                            TranscriptionJobName = job_name
                        });

                        var transcribeTextLocation = processing_response.TranscriptionJob.Transcript.TranscriptFileUri;

                        var webClient = new System.Net.WebClient();
                        webClient.DownloadFile(transcribeTextLocation, @"transcription.json");

                        break;
                    } else if (job_response.TranscriptionJob.TranscriptionJobStatus == TranscriptionJobStatus.FAILED) {
                        
                        Console.WriteLine("Job has failed.");
                        break;
                    }

                    Console.WriteLine("Job still processing...");
                    await Task.Delay(1000);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static async System.Threading.Tasks.Task Main(string[] args)
        {
           try {
                
                AmazonTranslateClient translateClient =
                new AmazonTranslateClient();

                var request = new TranslateTextRequest() {
                    Text = "Hallo, hoe gaan dit? Kan jy asseblief die lepel aangee?",
                    SourceLanguageCode = "auto",
                    TargetLanguageCode = "en"
                };

                var response = await translateClient.TranslateTextAsync(request);

                Console.WriteLine(response.TranslatedText);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
