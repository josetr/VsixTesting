$bin = Split-Path (Get-Command "msbuild").Path
$vsInstallRoot = "$bin\..\..\.."
&"$vsInstallRoot\Common7\IDE\devenv.exe" /RootSuffix Exp /ResetSettings General.vssettings /Command File.Exit | Out-Null