
@echo off
set H=R:\KSP_1.3.1_dev
echo %H%



copy /Y "KerbalHotSeat\bin\Debug\KerbalHotSeat.dll" "GameData\KerbalHotSeat\Plugins"
copy /Y KerbalHotSeat.version GameData\KerbalHotSeat

cd GameData
xcopy /y /s /i KerbalHotSeat "%H%\GameData\KerbalHotSeat"
