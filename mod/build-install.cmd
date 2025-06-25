chcp 65001
dotnet build -c Debug

copy /y ".\bin\Debug\net472\WorldLink.dll" "m:\Package\Mods"
rem call "e:\Maimai HDD\1.bat"

pushd m:\

pushd AMDaemon
start /min inject -d -k mai2hook.dll amdaemon.exe -f -c config_common.json config_server.json config_client.json
popd

pushd Package
sinmai.exe
pushd Package

taskkill /f /im amdaemon.exe

rem nya~