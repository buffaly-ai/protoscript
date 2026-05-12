call "%~dp0set_params.bat"
set LIB_ROOT=%ROOT%\lib\
set DEPLOY_ROOT=%ROOT%\Deploy\
set ONTOLOGY_DEPLOY=%ROOT%\..\ontology\Deploy\

if exist "c:\dev\RooTrax\RooTrax.Utilities\Deploy\BasicUtilities.dll" xcopy /y c:\dev\RooTrax\RooTrax.Utilities\Deploy\BasicUtilities.dll %LIB_ROOT%*.*
if exist "c:\dev\RooTrax\RooTrax.Utilities\Deploy\WebAppUtilities.dll" xcopy /y c:\dev\RooTrax\RooTrax.Utilities\Deploy\WebAppUtilities.dll %LIB_ROOT%*.*

if exist "c:\dev\RooTrax\RooTrax.Utilities\Deploy\RooTrax.Cache.dll" xcopy /y c:\dev\RooTrax\RooTrax.Utilities\Deploy\RooTrax.Cache.dll %LIB_ROOT%*.*
if exist "c:\dev\RooTrax\RooTrax.Utilities\Deploy\RooTrax.Common.dll" xcopy /y c:\dev\RooTrax\RooTrax.Utilities\Deploy\RooTrax.Common.dll %LIB_ROOT%*.*
if exist "c:\dev\RooTrax\RooTrax.Utilities\Deploy\RooTrax.Common.DB.dll" xcopy /y c:\dev\RooTrax\RooTrax.Utilities\Deploy\RooTrax.Common.DB.dll %LIB_ROOT%*.*

if exist "C:\dev\Buffaly.Development\Deploy\CSharp.dll" xcopy /y C:\dev\Buffaly.Development\Deploy\CSharp.dll %LIB_ROOT%*.*
if exist "C:\dev\Buffaly.Development\Deploy\CSharp.Parsers.dll" xcopy /y C:\dev\Buffaly.Development\Deploy\CSharp.Parsers.dll %LIB_ROOT%*.*

if exist "%ONTOLOGY_DEPLOY%Ontology.dll" xcopy /y "%ONTOLOGY_DEPLOY%Ontology*.dll" "%DEPLOY_ROOT%*.*"

exit /b 0
