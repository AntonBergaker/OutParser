$template_start = Get-Content -Path OutParser.CodeSource\TemplateStart.txt -Raw
$core = Get-Content -Path OutParser.CodeSource\OutParser.Core.cs -Raw

$content = "$template_start$core`"`"`";}"

Set-Content -Path OutParser.Generator\InitializationContent.cs -Value $content