[![progress-banner](https://backend.codecrafters.io/progress/http-server/34def3a5-9403-483c-aba3-c1272c655e95)](https://app.codecrafters.io/users/codecrafters-bot?r=2qF)

This is a starting point for C# solutions to the
["Build Your Own HTTP server" Challenge](https://app.codecrafters.io/courses/http-server/overview).

[HTTP](https://en.wikipedia.org/wiki/Hypertext_Transfer_Protocol) is the
protocol that powers the web. In this challenge, you'll build a HTTP/1.1 server
that is capable of serving multiple clients.

Along the way you'll learn about TCP servers,
[HTTP request syntax](https://www.w3.org/Protocols/rfc2616/rfc2616-sec5.html),
and more.

**Note**: If you're viewing this repo on GitHub, head over to
[codecrafters.io](https://codecrafters.io) to try the challenge.

# Passing the first stage

The entry point for your HTTP server implementation is in `src/Server.cs`. Study
and uncomment the relevant code, and push your changes to pass the first stage:

```sh
git commit -am "pass 1st stage" # any msg
git push origin master
```

Time to move on to the next stage!

# Stage 2 & beyond

Note: This section is for stages 2 and beyond.

1. Ensure you have `dotnet (8.0)` installed locally
1. Run `./your_program.sh` to run your program, which is implemented in
   `src/Server.cs`.
1. Commit your changes and run `git push origin master` to submit your solution
   to CodeCrafters. Test output will be streamed to your terminal.

# Local Testing

1. Get Echo Path: curl --verbose 127.0.0.1:4221/echo/abc
2. Get User Agent Path: curl -v --header "User-Agent: foobar/1.2.3" http://localhost:4221/user-agent
3. curl -v http://localhost:4221
4. curl -i -X GET http://localhost:4221/index.html
5. curl --verbose 127.0.0.1:4221/echo/abc
6. curl --verbose 127.0.0.1:4221/user-agent
7. curl -vvv -d "hello world" localhost:4221/files/readme.txt
