using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel;
using Discord;
using Discord.WebSocket;
using MyCouch;
using MyCouch.Requests;
using VideoLibrary;
using Discord.Audio;
using System.Diagnostics;

namespace DiscordMud {
    public class Spammable {
        public string VideoPath { get; }
        public ulong Length { get; }

        public Spammable(string videoPath, ulong length) {
            VideoPath = videoPath;
            Length = length;
        }

        public async static Task<Spammable> Create(string url) {
            var youtube = YouTube.Default;
            var video = youtube.GetVideo(url);
            var videoPath = Path.GetTempFileName();
            File.WriteAllBytes(videoPath, await video.GetBytesAsync());

            var process = Process.Start(new ProcessStartInfo {
                FileName = "ffprobe",
                Arguments = $"-v error -show_entries stream=duration -of default=noprint_wrappers=1:nokey=1 \"{videoPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });

            var taskCompletionSource = new TaskCompletionSource<object>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => taskCompletionSource.TrySetResult(null);
            await taskCompletionSource.Task;

            var length = process.StandardOutput.ReadToEnd().Split("\n").First();
            Console.WriteLine(length);

            return new Spammable(videoPath, (ulong)float.Parse(length));
        }

        private static Process CreateStream(string path) {
            return Process.Start(new ProcessStartInfo {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1 -filter:a \"volume=0.5\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
        }

        public async Task<bool> Spam(IAudioClient client) {
            bool success = true;
            try {
                using (var ffmpeg = CreateStream(VideoPath))
                using (var output = ffmpeg.StandardOutput.BaseStream)
                using (var discord = client.CreatePCMStream(AudioApplication.Mixed))
                {
                    try {
                        await output.CopyToAsync(discord); 
                    }
                    finally { 
                        await discord.FlushAsync(); 
                    }
                }
            }
            catch (Exception) {
                success = false;
            }

            return success;
        }
    }
}
