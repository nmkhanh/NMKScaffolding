$data = Get-Content all_4_instances_params.json | ConvertFrom-Json
$ids = ($data | Get-Member -MemberType NoteProperty).Name
# We can use python or revit mcp or a C# script to get the location curves of these 4 elements
