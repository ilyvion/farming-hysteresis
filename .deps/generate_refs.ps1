#Push-Location originals
Remove-Item -Recurse -Force refs
refasmer -v --all -O refs -g "originals\**\*.dll"
#Pop-Location
