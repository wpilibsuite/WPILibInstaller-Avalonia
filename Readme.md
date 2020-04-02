## How to publish


dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true


You can add the following to trim about 30 MB off of the resultant exe, but line numbers
will not show up anymore in stack traces. Tracked with the following issues.

/p:PublishTrimmed=true

https://github.com/dotnet/runtime/issues/33386
https://github.com/dotnet/runtime/issues/34187
