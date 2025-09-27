using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lotus.API.Player;
using Lotus.Chat;
using Lotus.Logging;
using Lotus.Network.PrivacyPolicy;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using VentLib.Commands;
using VentLib.Commands.Attributes;
using VentLib.Commands.Interfaces;
using VentLib.Localization.Attributes;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using IEnumerator = System.Collections.IEnumerator;

namespace Lotus.Network.Commands;

[Localized("Commands.BugReports")]
[Command("report")]
public class BugReportCommand : ICommandReceiver
{
    [Localized(nameof(WaitingOnReport))] public static string WaitingOnReport = "This report has been voided as we are waiting on a response for your previous report.";
    [Localized(nameof(FailedReport))] public static string FailedReport = "An error occured while sending the request. Please try again in a minute.\nError: {0}";
    [Localized(nameof(ReportingBug))] public static string ReportingBug = "Your request has went through and we are now reporting your bug. Thanks!";
    [Localized(nameof(ReportedBug))] public static string ReportedBug = "Your bug has been successfully reported. Again, thank you!";
    [Localized(nameof(ReportSuggest))] public static string ReportSuggest = "{0} suggests you to report \"{1}\".";
    [Localized(nameof(UploadingLog))] public static string UploadingLog = "Uploading log file... (Chunk {0}/{1})";

    private const string boundary = "---------------------------7d5e3d6f4509c";
    private const int CHUNK_SIZE = 4 * 1024 * 1024; // 4 MB in bytes

    private static readonly List<byte> reportingPlayerIds = new();
    public void Receive(PlayerControl source, CommandContext context)
    {
        if (!ProjectLotus.DevVersion)
        {
            ChatHandlers.NotPermitted().Send(source);
            return;
        }

        if (PrivacyPolicyInfo.Instance != null)
        {
            if (!PrivacyPolicyInfo.Instance.ConnectWithAPI) return;
        }
        if (reportingPlayerIds.Contains(source.PlayerId))
        {
            ChatHandlers.NotPermitted(WaitingOnReport).Send(source);
            return;
        }
        reportingPlayerIds.Add(source.PlayerId);
        ChatHandler.Of(ReportingBug).Send(source);

        Async.Execute(SendWebRequest(source, context));
    }

    private IEnumerator SendWebRequest(PlayerControl source, CommandContext context)
    {
        string message = string.Join(" ", context.Args);
        if (!source.IsHost())
        {
            ChatHandler.Of(ReportSuggest.Formatted(source.name, message)).Send(PlayerControl.LocalPlayer);
            reportingPlayerIds.Remove(source.PlayerId);
            yield break;
        }
        bool flag = PrivacyPolicyInfo.Instance != null && PrivacyPolicyInfo.Instance.AnonymousBugReports;
        if (source == PlayerControl.LocalPlayer && message == "log")
        {
            FileInfo? lastLogFile = GetLatestLogFile();
            if (lastLogFile == null)
            {
                LogManager.SendInGame("Could not find latest log file. Please make sure logs are enabled.");
                reportingPlayerIds.Remove(source.PlayerId);
                yield break;
            }

            byte[] fileData = File.ReadAllBytes(lastLogFile.FullName);
            string fileName = lastLogFile.Name;

            // Calculate number of chunks
            int totalChunks = (int)Math.Ceiling((double)fileData.Length / CHUNK_SIZE);
            bool success = true;
            string? logMessage = "";

            for (int chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
            {
                // Update user on upload progress
                ChatHandler.Of(UploadingLog.Formatted(chunkIndex + 1, totalChunks)).Send(source);

                // Calculate chunk size and offset
                int offset = chunkIndex * CHUNK_SIZE;
                int currentChunkSize = Math.Min(CHUNK_SIZE, fileData.Length - offset);

                // Create chunk data
                byte[] chunkData = new byte[currentChunkSize];
                Buffer.BlockCopy(fileData, offset, chunkData, 0, currentChunkSize);

                // Prepare multipart form data
                UnityWebRequest request = UnityWebRequest.Post(NetConstants.Host + "uploadlog", UnityWebRequest.kHttpVerbPOST);
                StringBuilder sb = new();
                sb.Append("--" + boundary + "\r\n");
                sb.Append("Content-Disposition: form-data; name=\"logFile\"; filename=\"" + fileName + ".part" + (chunkIndex + 1) + "of" + totalChunks + "\"\r\n");
                sb.Append("Content-Type: application/octet-stream\r\n\r\n");

                // Add additional metadata for chunked upload
                string metadataFooter = "\r\n--" + boundary + "\r\n" +
                    "Content-Disposition: form-data; name=\"hostName\"\r\n\r\n" + GetName() + "\r\n" +
                    "--" + boundary + "\r\n" +
                    "Content-Disposition: form-data; name=\"chunkIndex\"\r\n\r\n" + chunkIndex + "\r\n" +
                    "--" + boundary + "\r\n" +
                    "Content-Disposition: form-data; name=\"totalChunks\"\r\n\r\n" + totalChunks + "\r\n" +
                    "--" + boundary + "\r\n" +
                    "Content-Disposition: form-data; name=\"originalFileName\"\r\n\r\n" + fileName + "\r\n" +
                    "--" + boundary + "--\r\n";

                // Prepare the headers and file data
                byte[] headerBytes = Encoding.UTF8.GetBytes(sb.ToString());
                byte[] footerBytes = Encoding.UTF8.GetBytes(metadataFooter);

                // Combine header, chunk data, and footer
                byte[] bodyRaw = new byte[headerBytes.Length + chunkData.Length + footerBytes.Length];
                Buffer.BlockCopy(headerBytes, 0, bodyRaw, 0, headerBytes.Length);
                Buffer.BlockCopy(chunkData, 0, bodyRaw, headerBytes.Length, chunkData.Length);
                Buffer.BlockCopy(footerBytes, 0, bodyRaw, headerBytes.Length + chunkData.Length, footerBytes.Length);

                // Set up the request
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "multipart/form-data; boundary=" + boundary);

                // Send the request and wait for a response
                yield return request.SendWebRequest();

                // Handle the result for each chunk
                if (request.result != UnityWebRequest.Result.Success)
                {
                    success = false;
                    logMessage += "Result: {0}\nError: {1}\nResponseCode: {2}\nSever Response: {3}\n".Formatted(
                        request.result.ToString(), request.error, request.responseCode, request.downloadHandler.text);
                    request.Dispose();
                    break;
                }

                request.Dispose();
                yield return new WaitForSeconds(0.5f);
            }
            if (success)ChatHandler.Of(ReportedBug).Send(source);
            else ChatHandler.Of(FailedReport.Formatted(logMessage)).LeftAlign().Send(source);


            reportingPlayerIds.Remove(source.PlayerId);
            yield break;
        }

        if (string.IsNullOrEmpty(message) || message.Length < 7)
        {
            reportingPlayerIds.Remove(source.PlayerId);
            yield break;
        }
        FrozenPlayer frozenPlayer = new(source);
        string? errorMessage = null;

        // send report.

        string jsonData = $@"{{
            ""playerName"": ""{GetName()}"",
            ""friendCode"": ""{GetFriendCode()}"",
            ""message"": ""{message}""
        }}";
        byte[] postData = new System.Text.UTF8Encoding().GetBytes(jsonData);

        // Send POST request
        // Server checks for anon FC and sends it to the specified channel.
        UnityWebRequest webRequest = new(NetConstants.Host + "reportbug", UnityWebRequest.kHttpVerbPOST)
        {
            uploadHandler = new UploadHandlerRaw(postData),
            downloadHandler = new DownloadHandlerBuffer()
        };
        webRequest.SetRequestHeader("Content-Type", "application/json");

        yield return webRequest.SendWebRequest();

        switch (webRequest.result)
        {
            case UnityWebRequest.Result.Success:
                break;
            default:
                errorMessage = "Result: {0}\nError: {1}\nResponseCode: {2}\nSever Response: {3}".Formatted(webRequest.result.ToString(), webRequest.error,
                    webRequest.responseCode, webRequest.downloadHandler.text);
                break;
        }

        if (errorMessage == null) ChatHandler.Of(ReportedBug).Send(source);
        else ChatHandler.Of(FailedReport.Formatted(errorMessage)).Send(source);
        webRequest.Dispose();

        reportingPlayerIds.Remove(frozenPlayer.PlayerId);
        string GetName() => flag ? "Anonymous" : source.name;
        string GetFriendCode() => flag ? "anonymous#1234" : source.FriendCode;
    }

    public static FileInfo? GetLatestLogFile()
    {
        DirectoryInfo logsDirectory = new(LogDirectory.Directory);
        if (!logsDirectory.Exists) return null;
        FileInfo[] logs = logsDirectory.GetFiles();
        if (logs.Length == 0) return null;
        return logs
            .OrderByDescending(file => file.LastWriteTime)
            .FirstOrDefault();
    }
}