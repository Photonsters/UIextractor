@echo off
set /p file=UI FILENAME: 
set /p folder=EXTRACT FOLDER:
echo. 
echo extracting... %file% to %folder%
echo. 
UIextractor.exe %file% %folder%
echo.
echo done.
echo.
pause