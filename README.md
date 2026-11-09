# Universal Mouse Studio 0.1.0

Aplicação desktop C#/.NET 10 e WPF para Windows 10/11 x64. Esta entrega contém código-fonte, build Release e pacote portátil com o runtime incluído. **As integrações Windows foram compiladas em Linux, mas ainda não foram executadas em Windows nem validadas em mouse físico.** Não distribua o modo de correção experimental como produto validado antes do roteiro de ensaios.


## Download para o Windows

Abra [Releases](https://github.com/Hiago-sys/Universal-Mouse-Studio/releases/latest) e baixe **UniversalMouseStudio-completo.zip** para obter fontes, recursos, configurações e aplicativo com runtime. A primeira Release só aparecerá depois de o [build Windows](https://github.com/Hiago-sys/Universal-Mouse-Studio/actions) terminar com sucesso.

Extraia o ZIP para `%USERPROFILE%\Desktop`; ele contém a pasta `UniversalMouseStudio`. O executável fica em `UniversalMouseStudio\artifacts\portable-win-x64\UniversalMouseStudio.exe`. Para Área de Trabalho no OneDrive, selecione o destino correspondente no Explorador. Nada é copiado automaticamente para seu computador.

## Executar

1. Extraia **todos** os arquivos do pacote `UniversalMouseStudio-win-x64.zip` para uma pasta gravável.
2. Abra `UniversalMouseStudio.exe` no Windows, como usuário normal. O pacote self-contained dispensa instalar .NET.
3. A aplicação inicia com o hook desligado. Consulte Dispositivos e releia os ajustes reais do Windows.
4. No laboratório, clique em Capturar/pausar e mova o mouse selecionado. Verde é o resultado matemático; cinza é Raw Input.
5. Para experimentar a alteração real do cursor, use **Ativar correção do cursor**. Ela é global e atua no cursor desktop, sem substituir o Raw Input dos jogos. **Ctrl+Alt+F11** desliga o processamento.

O executável não está assinado. Não são instalados drivers, serviços, telemetria ou componentes de inicialização. Configurações aplicadas ao Windows persistem após fechar; use **Restaurar valores da abertura** antes de fechar para desfazê-las. Perfis não guardam autorização de ativação do hook.

## Implementado

- Motor determinístico de correção angular: tolerância, intensidade/eixos, diagonais, histerese, distância de aquisição, suavização e resíduos de arredondamento.
- WPF/MVVM, 11 seções, tema claro/escuro, interface em português e controles navegáveis por teclado.
- Enumeração Raw Input, leitura de metadados HID/SetupAPI/Configuration Manager, notificações de conexão e múltiplos dispositivos.
- Preferências globais reais via Win32: velocidade, aceleração, botões principais, clique duplo, ClickLock, linhas/página e rolagem inativa. Leitura de confirmação e tentativa de rollback.
- Hook opcional em thread dedicada, rejeição de eventos injetados, reinjeção absoluta e falha que libera a entrada original; atalho de emergência. Sem ativação automática.
- Remapeamento opcional X1/X2: copiar, colar, recortar, desfazer, volume, play/pause. Todos os mouses compartilham o hook.
- Perfis JSON com criação, duplicação, renomeação, exclusão, importação/exportação, recuperação e troca por executável, opcional e global.
- Laboratório limitado a 3000 amostras em memória, reprodução sem reinjeção, teste de frequência de eventos, calibração aproximada por distância, teste de clique/scroll e relatórios agregados.
- Contratos de adaptadores e classificação de compatibilidade que recusam comandos físicos sem protocolo conhecido.

**Não implementado:** RGB físico/efeitos, DPI/polling/debounce físicos, LOD/Motion Sync, driver de filtro, inversão de scroll, abertura de programas por botão, tradução adicional e atualizador automático. Há estrutura Inno Setup, mas não um instalador compilado/assinado. Sensor de mouse genérico não ganha capacidades físicas inexistentes.

## Fonte e build

Requer SDK .NET 10. A fonte não usa pacotes NuGet externos; restore baixa packs oficiais da Microsoft.

```powershell
./scripts/build.ps1
./scripts/publish.ps1
# Somente benchmark matemático:
dotnet run --project tests/UniversalMouse.Tests -c Release -- --benchmark
```

No Linux, o cross-build usa `EnableWindowsTargeting`; não há execução WPF. Em Windows, abra `UniversalMouseStudio.sln` no Visual Studio compatível com .NET 10 ou execute os scripts. O SDK pode resolver a atualização 10.0.xxx instalada usando `global.json`.

## Estrutura

```
src/UniversalMouse.Core/     motor, perfis, capacidades e diagnósticos portáveis
src/UniversalMouse.Windows/ Raw Input, HID, Win32 e hook opcional
src/UniversalMouse.App/     WPF, MVVM, laboratório e composição de serviços
tests/                     executáveis de testes e benchmark sem framework externo
native/OptionalInputFilter/ estudo técnico; nenhum driver
scripts/                   build, publish e estrutura Inno Setup
docs/                      arquitetura, limites, testes e matriz da entrega
```

A estrutura agrupa serviços relacionados em três assemblies para reduzir complexidade sem misturar o motor com APIs do Windows. Injeção de serviços é manual na composição; não há contêiner de DI externo.

## Dados e remoção

Perfis ficam em `%LOCALAPPDATA%\UniversalMouseStudio`, com backup e recuperação de corrupção. Não há trajetórias gravadas no disco nem captura de teclas digitadas. O teste de laboratório só mantém dados transitórios. Feche a aplicação e apague a pasta portátil para removê-la; apague a pasta de dados apenas se desejar excluir os perfis. Restaure os ajustes globais antes de remover se não quiser mantê-los. O desinstalador opcional pergunta antes de remover perfis.

## Evidências e limitações

- [Resultados medidos e build](docs/BUILD_RESULTS.md)
- [Matriz de aceitação](docs/STATUS.md)
- [Arquitetura](docs/ARCHITECTURE.md)
- [Compatibilidade](docs/COMPATIBILITY.md)
- [Angle Snapping](docs/ANGLE_SNAPPING.md)
- [Driver opcional e Moufiltr](docs/DRIVER_REQUIREMENTS.md)
- [Roteiro de testes Windows](docs/TESTING.md)
- [Guia de uso](docs/USER_GUIDE.md)

A fase de Angle Snapping permanece **parcialmente implementada** no sentido dos critérios de aceitação: algoritmo e aplicação via Win32 existem, mas ensaio real de cursor, recuperação e consumidores Raw Input está pendente. Não há promessa de compatibilidade com jogos ou anticheats. O REA foi preservado em seu repositório separado; este aplicativo é código novo e não depende dele em execução.
