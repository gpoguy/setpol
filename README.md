# setpol
Lets you write arbitrary registry entries to Group Policy related .pol files (e.g. registry.pol).

Command-line utility setpol.exe takes a set of arguments for passing registry entries into a pol file, including:

arg1: policy file path

arg2: registry key path

arg3: registry value type (e.g. REG_SZ, REG_DWORD)

arg4: registry value name

arg5: registry data

For example:

setpol.exe "\\cpandl.com\sysvol\cpandl.com\policies\{0EB0D8E0-2599-48AC-A7C4-18099BABFC95}\Machine\registry.pol" "Software\Policies\Windows\System" "REG_DWORD" "AllowX-ForestPolicy-and-RUP" 1
