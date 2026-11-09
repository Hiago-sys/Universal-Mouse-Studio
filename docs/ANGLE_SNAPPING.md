# Angle Snapping — estado e método

**Estado:** algoritmo independente implementado; mecanismo de cursor em modo usuário implementado, com validação real no Windows pendente. Não se deve marcar a fase concluída enquanto os ensaios Windows de ativação, desativação e recuperação não forem executados.

## Motor determinístico

`MotionProcessor` aceita deltas relativos e um timestamp em milissegundos. Não tem temporizadores próprios nem produz movimentos sem entrada. As configurações são validadas, e valores não finitos ou acima dos limites são rejeitados.

1. Uma evidência vetorial com decaimento espacial estima a direção recente.
2. Após a distância mínima configurada, a direção é comparada às linhas habilitadas: horizontal, vertical e diagonais opcionais.
3. A linha só é adquirida se tanto a direção estimada quanto a amostra atual respeitarem a tolerância de entrada.
4. Uma margem adicional de histerese mantém a linha até a tolerância de saída. Portanto, uma amostra entre tolerância de entrada e saída pode continuar corrigida quando a linha já foi adquirida.
5. A projeção vetorial na linha é interpolada pela intensidade global e pela intensidade do eixo. Intensidade zero e suavização zero preservam a entrada.
6. Mudanças deliberadas de direção liberam a linha. Reversões, interrupções, timestamps regressivos e reconfiguração reinicializam o estado.

A suavização é uma opção separada, desativada por padrão; ela interpola deltas e pode alterar a distância percorrida durante mudanças de velocidade. Nenhum temporizador reproduz uma cauda depois que o mouse para.

`MotionQuantizer` preserva o resto fracionário de arredondamento para saída em pixels inteiros. O componente perpendicular intencionalmente removido pelo snapping **não** é acumulado para reaparecer depois: isso causaria saltos e contrariaria a correção. Portanto, conservação exata do vetor original não é uma promessa do recurso. Ao desligar/reconfigurar, restos e estado devem ser descartados.

## Parâmetros

| Parâmetro | Intervalo do motor |
| --- | --- |
| Intensidades global/horizontal/vertical | 0–1 (interface: 0–100%) |
| Tolerância | 0–30° |
| Histerese adicional | 0–10° |
| Diagonal | 1–89°, com linha espelhada em 180° − ângulo |
| Suavização | 0–0,9 |
| Distância mínima | 0–100 unidades de entrada |
| Intervalo para reset | 1–2000 ms |

Diagonais e suavização são desativadas por padrão. A sensibilidade de detecção é uma distância, não uma alteração de DPI.

## Aplicação ao cursor

O hook de baixo nível observa coordenadas de tela, transforma o deslocamento e solicita uma posição absoluta normalizada sobre o desktop virtual. O evento original só deve ser suprimido após aceitação da reinjeção. Eventos já injetados passam sem nova transformação. Uma falha desativa o processamento e libera a entrada original. Cliques e roda não são transformados pelo motor.

O retorno de `SendInput` indica eventos aceitos na fila, não comprova consumo pelo aplicativo. A correção não é por dispositivo, não substitui Raw Input, não altera o sensor e não garante comportamento em jogos. O Windows pode remover silenciosamente hooks lentos; uma indicação interna de serviço ativo não comprova que todo consumidor esteja recebendo movimento corrigido. A validação com monitoramento externo continua necessária.

## Laboratório

Compare trajetórias bruta e transformada no espaço do laboratório. Esse ensaio avalia o motor, com até 3000 amostras em memória. Identifique separadamente o estado do processamento global. Não use a diferença entre duas linhas desenhadas como prova de modificação da entrada de outro programa.

## Recuperação

- Ctrl+Alt+F11 desliga o processamento de cursor.
- Desligar limpa estado de movimento e remove o hook.
- Exceções no caminho de entrada devem falhar em modo aberto.
- Não há driver instalado; terminar o processo remove os hooks associados.
- Nenhuma alteração de sensor precisa ser revertida.

Fontes oficiais: [LowLevelMouseProc](https://learn.microsoft.com/en-us/windows/win32/winmsg/lowlevelmouseproc), [Raw Input](https://learn.microsoft.com/en-us/windows/win32/inputdev/about-raw-input), [SendInput](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput), [MOUSEINPUT](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-mouseinput).
