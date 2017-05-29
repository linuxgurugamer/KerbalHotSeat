
set H=R:\KSP_1.3.0_dev
echo %H%



copy /Y "plugins\KerbalHotSeat\bin\Debug\KerbalHotSeat.dll" "GameData\KerbalHotSeat\Plugins"
copy /Y KerbalHotSeat.version GameData\KerbalHotSeat

cd GameData
mkdir "%H%\GameData\Fusebox"
xcopy /y /s KerbalHotSeat "%H%\GameData\KerbalHotSeat"
pause