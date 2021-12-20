Website
http://github.com/superlloyd/MenuRibbon

About Ribbon
http://msdn.microsoft.com/en-us/library/dn742393.aspx
http://msdn.microsoft.com/en-us/library/ff701790(v=vs.110).aspx
http://msdn.microsoft.com/EN-US/library/hh140095(v=VS.110,d=hv.2).aspx

TODO
Better looking top-level menu item  and KeyTip
update doc about KeyTips and ItemTemplate
Make sure KeyTip work on collapsed item
Replace Font by Path for Pin button, font not supported on system without Word
Big Item Button => splitter below


How to Publish
==============
0] Manage API keys @ https://www.nuget.org/account/apikeys
1] https://docs.microsoft.com/en-us/nuget/create-packages/creating-a-package-dotnet-cli
2] https://docs.microsoft.com/en-us/nuget/nuget-org/publish-a-package
3] run those commands

dotnet pack  -c Release .\MenuRibbon\MenuRibbon.WPF.csproj
cd .\MenuRibbon\bin\Release\
..\..\..\..\nuget push .\MenuRibbon.WPF.1.1.0.nupkg -Source https://api.nuget.org/v3/index.json -ApiKey  '=== INSERT API KEY HERE ==='

