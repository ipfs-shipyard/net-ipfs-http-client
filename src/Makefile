include .env
nuget-push:
	nuget push bin/Release/$(PACKAGE_NAME).$(VERSION).nupkg -Source https://api.nuget.org/v3/index.json -ApiKey $(NUGET_API_KEY)
nuget-pack:
	nuget pack $(PACKAGE_NAME).nuspec -Version $(VERSION) -Properties Configuration=Release -OutputDirectory bin/Release
