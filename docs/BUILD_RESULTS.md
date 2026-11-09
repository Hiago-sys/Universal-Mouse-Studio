# Evidências da entrega 0.1.0

Registro histórico da primeira entrega. Para a correção 0.1.1, veja `ANGLE_SNAPPING_FIX_0.1.1.md`; os números abaixo não são os totais da revisão mais recente.

## Ambiente

- Compilação em Linux x64, kernel 6.18.57; SDK .NET 10.0.403, runtime de testes 10.0.14.
- Target desktop `net10.0-windows`, WPF, publicação `win-x64`, self-contained sem trimming/ReadyToRun.
- Não há Windows, WDK nem mouse Windows físico neste ambiente. O executável não foi iniciado aqui. Compilar XAML/C# não confirma renderização, P/Invoke ou comportamento de hardware.

## Resultado

- Solução Release: **0 erros, 0 avisos**.
- Movimento/coordenadas/quantização: **24/24 testes aprovados**.
- Perfis/capacidades/diagnósticos: **19/19 testes aprovados**.
- Total automatizado: **43 testes aprovados**.
- Publicação self-contained Windows x64: concluída, com `UniversalMouseStudio.exe` e DLLs do runtime desktop.
- Testes Windows manuais: **não executados**; roteiro em TESTING.md.
- Instalador Inno Setup: somente fonte/estrutura; não compilado, não assinado.

## Benchmark reproduzível

Execução Release de 2.000.000 eventos, após aquecimento, em relógio monotônico:

| Medida | Resultado observado |
| --- | ---: |
| Tempo total | 0,4395774 s |
| Custo médio | 219,7887 ns/evento |
| Alocações gerenciadas no trecho medido | 0 bytes |
| Checksum | 21.000.000 |

É uma execução neste ambiente, não uma garantia de desempenho em outras máquinas. Não mede USB, hardware, coleta Raw Input, hook, SendInput, interface ou latência ponta a ponta. Não há alegação de melhoria de latência.

## Comandos

```sh
dotnet build UniversalMouseStudio.sln -c Release
dotnet run --project tests/UniversalMouse.Tests -c Release --no-build -- --benchmark
dotnet run --project tests/UniversalMouse.ProfileTests -c Release --no-build
dotnet publish src/UniversalMouse.App/UniversalMouse.App.csproj -c Release -r win-x64 --self-contained true -p:PublishReadyToRun=false -o artifacts/portable-win-x64
```

Os logs de build/teste/publicação são incluídos em `docs/evidence/`. Os runners de teste são executáveis sem framework externo; use `dotnet run`, não `dotnet test`, que não executaria estes testes. O modo de filtro opcional não foi construído. A revisão de normalização de coordenadas e resíduos originou quatro regressões efetivamente executadas.
