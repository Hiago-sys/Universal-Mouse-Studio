# Testes do motor de movimento

Execute na raiz com o SDK .NET 10:

```sh
dotnet run --project tests/UniversalMouse.Tests -c Release
dotnet run --project tests/UniversalMouse.Tests -c Release -- --benchmark
```

O programa retorna código 1 se qualquer verificação falhar. Não depende de pacotes de teste externos. O benchmark aquece o JIT, transforma dois milhões de eventos e informa tempo, arquitetura, versão do runtime, checksum e alocações da thread. O tempo é medido por `Stopwatch`; o console não participa do trecho medido. Não equivale à latência do cursor, a desempenho USB ou a validação de hardware.

Cobertura: retas horizontais e verticais em ambos os sentidos, ruído pequeno, desvios grandes, diagonal desligada e ativa, círculo, velocidades, aquisição após limiar, trajetória curta, mudanças de direção, pausa, alternância ativa/inativa, forças independentes, histerese, resíduos de quantização, passthrough após resíduos de ±0,5, normalização de coordenadas de desktop, entradas inválidas, determinismo e alocações.

O teste de coordenadas verifica cada pixel em dimensões de 2 a 65.536 pixels selecionadas, incluindo origens negativas. Ele usa o modelo de seleção `floor(normalized * extent / 65536)`; não substitui um teste real de `SendInput` no Windows nem valida lacunas entre monitores.

O algoritmo remove intencionalmente parte do deslocamento perpendicular à linha escolhida. Conservar o deslocamento bidimensional integral seria incompatível com essa correção. `MotionQuantizer` conserva apenas frações de arredondamento; não acumula o desvio removido para devolvê-lo como saltos posteriores. A suavização é independente, desativada por padrão e pode mudar o comprimento acumulado. Um evento zero ou uma pausa acima do limite limpa o histórico; não existe gerador de movimento após a parada.

As classes são de consumidor único. Chame `Configure` na mesma thread que `Process`, ou sincronize externamente. Ao desativar, trocar perfil ou abortar uma sequência de entrada, limpe também o quantizador. A integração utiliza `QuantizeOrPassthrough` para não transportar resíduos antigos para um movimento inteiro sem transformação. Exceções de validação devem fazer a integração com o Windows desativar a correção e repassar a entrada original.
