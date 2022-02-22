# Dolphin: An implementation of Moving Actor-Oriented Databases (AODBs)

author: wangyiwen0304@gmail.com

ref page: https://di.ku.dk/english/research/phd/phd-theses/2021/Yiwen_Wang_PhDThesis_VLD772.pdf

Software dependencies

Environment configurations

Build it

Run it
	dotnet run --project [FOLDER_PATH]\OrleansSiloHost\SiloHost.csproj
	dotnet run --project [FOLDER_PATH]\Experiment.Controller\Experiment.Controller.csproj -usingLearnedIndex 1 -indexType h -distribution g -semantics f -queryrate 0 -reactivesensing 0.125 -interval 1 -hotspotnum 1 //
	dotnet run --project [FOLDER_PATH]\Experiment.Process\Experiment.Process.csproj

Run example

System-wide runtime options

Benchmark-specific runtime options
