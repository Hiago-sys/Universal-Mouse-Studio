Correção do laboratório de Angle Snapping:

- Regressão reproduzida antes da correção: 0/80 desvios pequenos corrigidos numa trajetória inteira quase horizontal.
- Pacotes pequenos não desfazem imediatamente uma direção estável; viradas deliberadas são liberadas com perda limitada de até duas contagens nesse caso ambíguo.
- Frações de intensidades intermediárias sobrevivem a eventos alinhados.
- Contagem explícita de amostras corrigidas, estados de captura e limite; seleção do mouse pelo movimento.
- Ajustes recalculam a comparação já capturada. O hook não precisa estar ativo para testar no laboratório.
- 30 testes de movimento + 19 de perfis/capacidades/diagnósticos + 9 do controle WPF no Windows, incluindo renderização com dados sintetizados. Testes automáticos não são validação em mouse físico.

Teste sugerido: Forte → Aplicar algoritmo no laboratório → Escolher mouse pelo movimento → traçar linha quase horizontal e verificar amostras/corrigidas. Se as amostras continuarem em zero ou não houver correção, isso precisa ser investigado, não tratado como sucesso.

Versão experimental 0.1.1 do Universal Mouse Studio, WPF/.NET 10 para Windows x64.

**Para obter tudo:** baixe `UniversalMouseStudio-completo.zip`. Ele contém a pasta raiz UniversalMouseStudio, o código-fonte, recursos XAML, configurações, testes, scripts, documentação e o aplicativo com runtime em `artifacts/portable-win-x64/`.

Extraia para sua Área de Trabalho. No PowerShell, se o ZIP estiver em Downloads:

```powershell
Expand-Archive -LiteralPath "$env:USERPROFILE\Downloads\UniversalMouseStudio-completo.zip" -DestinationPath "$env:USERPROFILE\Desktop"
```

Em uma Área de Trabalho redirecionada para OneDrive, use `[Environment]::GetFolderPath('Desktop')` como destino. Não use `-Force` sobre um projeto existente sem revisar os arquivos.

Depois abra `UniversalMouseStudio\artifacts\portable-win-x64\UniversalMouseStudio.exe`, preservando os arquivos ao lado. Não é necessário instalar .NET. `SHA256SUMS.txt` permite conferir os downloads.

Alternativas: `UniversalMouseStudio-win-x64.zip` contém somente aplicativo/runtime/documentação; `UniversalMouseStudio-source.zip` contém somente o projeto-fonte.

O workflow compila no Windows e executa os testes portáveis. Isso **não comprova** execução da interface nem integração em mouse físico. O hook permanece experimental e desligado inicialmente; Ctrl+Alt+F11 desativa. RGB físico, driver e alterações físicas de DPI/polling não estão implementados. O relatório e a matriz em docs detalham os limites. Nenhum arquivo foi automaticamente transferido à Área de Trabalho do usuário.
