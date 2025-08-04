pnpm install

if (Test-Path .\wwwroot\lib\webawesome) {
    Remove-Item -Path .\wwwroot\lib\webawesome -Force -Recurse
}
Copy-Item '.\node_modules\@awesome.me\webawesome\dist-cdn' -Destination '.\wwwroot\lib\webawesome' -Recurse -Exclude *.d.ts

pnpm tailwindcss -i.\wwwroot\css\input.css -o.\wwwroot\css\output.css

if (Test-Path) {
    Remove-Item -Path .\bin\publish -Force -Recurse
}

dotnet publish -p:PublishSingleFile=true -p:SelfContained=false -r win-x64 -o .\bin\publish
