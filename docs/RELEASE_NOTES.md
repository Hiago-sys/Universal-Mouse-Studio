Versão inicial experimental do Universal Mouse Studio, WPF/.NET 10 para Windows x64.

**Para obter tudo:** baixe `UniversalMouseStudio-completo.zip`. Ele contém a pasta raiz UniversalMouseStudio, o código-fonte, recursos XAML, configurações, testes, scripts, documentação e o aplicativo com runtime em `artifacts/portable-win-x64/`.

Extraia para sua Área de Trabalho. No PowerShell, se o ZIP estiver em Downloads:

```powershell
Expand-Archive -LiteralPath "$env:USERPROFILE\Downloads\UniversalMouseStudio-completo.zip" -DestinationPath "$env:USERPROFILE\Desktop"
```

Em uma Área de Trabalho redirecionada para OneDrive, use `[Environment]::GetFolderPath('Desktop')` como destino. Não use `-Force` sobre um projeto existente sem revisar os arquivos.

Depois abra `UniversalMouseStudio\artifacts\portable-win-x64\UniversalMouseStudio.exe`, preservando os arquivos ao lado. Não é necessário instalar .NET. `SHA256SUMS.txt` permite conferir os downloads.

Alternativas: `UniversalMouseStudio-win-x64.zip` contém somente aplicativo/runtime/documentação; `UniversalMouseStudio-source.zip` contém somente o projeto-fonte.

O workflow compila no Windows e executa os testes portáveis. Isso **não comprova** execução da interface nem integração em mouse físico. O hook permanece experimental e desligado inicialmente; Ctrl+Alt+F11 desativa. RGB físico, driver e alterações físicas de DPI/polling não estão implementados. O relatório e a matriz em docs detalham os limites. Nenhum arquivo foi automaticamente transferido à Área de Trabalho do usuário.
