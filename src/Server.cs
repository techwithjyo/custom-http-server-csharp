using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();
Console.WriteLine("Server started on port 4221");

while (true)
{
    // Accept a client socket
    var socket = server.AcceptSocket();
    Console.WriteLine("Client connected");

    // Handle the client connection in a new thread
    ThreadPool.QueueUserWorkItem(HandleClient, socket);
}

void HandleClient(object obj)
{
    var socket = (Socket)obj;
    try
    {
        using (var networkStream = new NetworkStream(socket))
        using (var reader = new StreamReader(networkStream, Encoding.UTF8))
        {
            string requestLine = reader.ReadLine();
            var (method, path) = ParseRequestLine(requestLine);
            Console.WriteLine($"Received request: {requestLine}");

            string requestedPath = requestLine.Split(' ')[1];
            Console.WriteLine($"Requested path: {requestedPath}");

            // Read headers
            string userAgent = null;
            string acceptEncoding = null;
            string line;
            int contentLength = 0;

            while (!string.IsNullOrEmpty(line = reader.ReadLine()))
            {
                if (line.StartsWith("User-Agent:"))
                {
                    userAgent = line.Substring("User-Agent:".Length).Trim();
                    Console.WriteLine($"Extracted User-Agent: {userAgent}");
                }
                else if (line.StartsWith("Accept-Encoding:"))
                {
                    acceptEncoding = line.Substring("Accept-Encoding:".Length).Trim();
                    Console.WriteLine($"Extracted Accept-Encoding: {acceptEncoding}");
                }
                else if (line.StartsWith("Content-Length:"))
                {
                    contentLength = int.Parse(line.Substring("Content-Length:".Length).Trim());
                    Console.WriteLine($"Content-Length: {contentLength}");
                }
            }


            if (method == "GET")
            {
                if (requestedPath == "/")
                {
                    Send200OkayResponse(socket);
                }
                else if (requestedPath.StartsWith("/echo/"))
                {
                    SendEchoPathResponse(socket, requestedPath, acceptEncoding);
                }
                else if (requestedPath == "/user-agent" && userAgent != null)
                {
                    SendUserAgentPathResponse(socket, userAgent);
                }
                else if (requestedPath.StartsWith("/files/"))
                {
                    string directory = GetDirectory();
                    ReturnBackFile(socket, requestedPath, directory);
                }
                else
                {
                    Send404NotFoundResponse(socket);
                }
            }
            else if (method == "POST" && path.StartsWith("/files/"))
            {
                PostFilesFromHttpServer(socket, reader, path, contentLength); 
                
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error handling client: {ex.Message}");
    }
    finally
    {
        socket.Close();
        Console.WriteLine("Client disconnected");
    }
}

void PostFilesFromHttpServer(Socket socket, StreamReader reader, string path, int contentLength)
{
    // Read the request body
    char[] buffer = new char[contentLength];
    reader.Read(buffer, 0, contentLength);
    string requestBody = new string(buffer);
    Console.WriteLine($"Request Body: {requestBody}");

    string directory = GetDirectory();
    // Write to file
    string fileName = path.Substring("/files/".Length);
    string filePath = Path.Combine(directory, fileName);
    Console.WriteLine("FilePath: " + filePath);

    File.WriteAllText(filePath, requestBody);
    Console.WriteLine("File created with content!");

    string response = "HTTP/1.1 201 Created\r\n\r\n";
    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
    socket.Send(responseBytes);
    Console.WriteLine("Sent response 201 Created!");

    // Ensure the response is flushed
    socket.Shutdown(SocketShutdown.Send);
    Console.WriteLine("Socket shutdown for sending.");
}

void ReturnBackFile(Socket socket, string requestedPath, string directory)
{
    string fileName = requestedPath.Substring("/files/".Length);
    string filePath = Path.Combine(directory, fileName);
    Console.WriteLine("FilePath: " + filePath);

    if (File.Exists(filePath))
    {
        byte[] fileContent = File.ReadAllBytes(filePath);

        string response = $"HTTP/1.1 200 OK\r\n" +
                           "Content-Type: application/octet-stream\r\n" +
                           $"Content-Length: {fileContent.Length}\r\n\r\n";
        socket.Send(Encoding.UTF8.GetBytes(response));
        socket.Send(fileContent);
        Console.WriteLine("Sent response 200 OK with file content!");
    }
    else
    {
        string response = "HTTP/1.1 404 Not Found\r\n\r\n";
        socket.Send(Encoding.UTF8.GetBytes(response));
        Console.WriteLine("Sent response 404 Not Found!");
    }
}

string GetDirectory()
{
    string directory = null;
    string[] args = Environment.GetCommandLineArgs();
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "--directory" && i + 1 < args.Length)
        {
            directory = args[i + 1];
            Console.WriteLine("Directory: " + directory);
            return directory;
        }
    }
    return null;
}
void SendUserAgentPathResponse(Socket socket, string userAgent)
{
    string response = $"HTTP/1.1 200 OK\r\n" +
                                        "Content-Type: text/plain\r\n" +
                                        $"Content-Length: {userAgent.Length}\r\n\r\n" +
                                        userAgent;
    socket.Send(Encoding.UTF8.GetBytes(response));
    Console.WriteLine("Sent response 200 OK with User Agent!");
}

void SendEchoPathResponse(Socket socket, string requestedPath, string acceptEncoding)
{
    string echoPath = requestedPath.Substring("/echo/".Length);
    Console.WriteLine($"Extracted echo path: {echoPath}");

    byte[] responseBodyBytes = Encoding.UTF8.GetBytes(echoPath);

    string contentEncodingHeader = "";
    if (acceptEncoding != null && acceptEncoding.Split(',').Select(e => e.Trim()).Contains("gzip"))
    {
        using (var memoryStream = new MemoryStream())
        {
            using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
            {
                gzipStream.Write(responseBodyBytes, 0, responseBodyBytes.Length);
            }
            responseBodyBytes = memoryStream.ToArray();
        }
        contentEncodingHeader = "Content-Encoding: gzip\r\n";
    }

    string response = $"HTTP/1.1 200 OK\r\n" +
                       "Content-Type: text/plain\r\n" +
                       contentEncodingHeader +
                       $"Content-Length: {responseBodyBytes.Length}\r\n\r\n";
    socket.Send(Encoding.UTF8.GetBytes(response));
    socket.Send(responseBodyBytes);
    Console.WriteLine("Sent response 200 OK with echo path!");
}

void Send404NotFoundResponse(Socket socket)
{
    // Respond with 404 Not Found
    string response = "HTTP/1.1 404 Not Found\r\n\r\n";
    socket.Send(Encoding.UTF8.GetBytes(response));
    Console.WriteLine("Sent response 404 Not Found!");
}

(string method, string path) ParseRequestLine(string? requestLine)
{
    if (requestLine != null)
    {
        string[] requestParts = requestLine.Split(' ');
        string method = requestParts[0];
        string path = requestParts[1];
        Console.WriteLine($"Method: {method}, Path: {path}");
        return (method, path);
    }
    return (string.Empty, string.Empty);
}

void Send200OkayResponse(Socket socket)
{
    string response = "HTTP/1.1 200 OK\r\n\r\n";
    socket.Send(Encoding.UTF8.GetBytes(response));
    Console.WriteLine("Sent response 200 OK!");
}