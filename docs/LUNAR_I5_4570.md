# Baseline para Lunar Client / Minecraft 1.8.9

Configuração informada pelo usuário: Core i5-4570, vídeo integrado (Intel HD Graphics 4600 nessa CPU), 16 GB RAM, monitor 60 Hz, sensibilidade Minecraft 76 e DPI desconhecido. As sugestões são hipóteses de ajuste para comparar, não resultados medidos nesse computador.

## Teste de imagem e estabilidade

Fotografe ou anote os ajustes atuais antes de mudar. Compare por 60 segundos no mesmo local de um mundo local e depois no mesmo servidor, sem mudar várias opções durante a medição.

Ponto de partida: gráficos Fast/Rápidos; 6 chunks (compare 4 se continuar pesado); nuvens desligadas; iluminação suave desligada; partículas reduzidas; sombras de entidades desligadas; texturas padrão/16x e nenhum shader. Se existir opção VBO, teste ligada. Comece com limite de 60 FPS e VSync desligado; pode ocorrer tearing. Depois compare limite 90 para decidir entre estabilidade, carga da GPU e resposta percebida. Monitor de 60 Hz não implica que todo limite acima de 60 seja inútil.

Compare tela cheia em 1280x720 com a resolução atual. Uma melhora grande ao reduzir resolução sugere carga gráfica relevante, mas não prova sozinha um gargalo exclusivo. Se ficar estável e a imagem piorar demais, aumente a resolução e repita. Registre FPS mínimo observado e quedas; o contador médio não descreve todos os engasgos. A ferramenta deve ser usada com o jogo real, não com números inventados.

Com 16 GB, 3 GB de heap é um ponto inicial razoável para comparar nesse cliente; ajuste pelo menu do Lunar se disponível. Compare 4 GB apenas se houver indício de pressão do heap. Mais RAM alocada não garante mais FPS nem menos pausas. Mantenha o runtime gerenciado pelo Lunar e não adicione pacotes de argumentos JVM sem medições. Não há mudança automática de memória ou arquivo do cliente neste projeto.

## Separar travamento de conexão

Se a câmera e a animação também param e o FPS cai, investigue desempenho local. Se a câmera continua fluida, mas jogadores teleportam e o ping varia, investigue rede/servidor. Um congelamento local também pode produzir um salto aparente depois; ambos os sintomas não comprovam duas causas independentes.

Compare mundo local versus servidor. Se usa Wi-Fi, compare cabo de rede quando possível e interrompa downloads concorrentes durante o teste. Escolha uma região de servidor próxima quando houver essa opção. Nenhum ajuste gráfico elimina latência geográfica ou atraso do servidor. Ping não é medido pelo script diagnóstico.

## Mira

Mantenha sensibilidade 76 inicialmente para isolar mudanças gráficas. DPI desconhecido impede comparar sensibilidade física com outros jogadores. Use uma régua em um ambiente de treino e meça a distância de um giro de 360 graus; repita na mesma velocidade e sem trocar a posição do botão DPI. Não é leitura do DPI do sensor.

Depois compare 76 com 65 em treino, mantendo o restante igual. Avalie se ultrapassa o alvo e se consegue corrigir movimentos pequenos. Não há número ideal universal. Angle Snapping pode ajudar a retificar uma linha e atrapalhar microajustes intencionais; para PvP compare Desativado e Leve antes de usar Forte. Confira as regras do servidor para ferramentas de processamento de entrada.

O modo desktop do Universal Mouse Studio ainda não foi validado no Lunar: se ele usa entrada direta, o hook pode não afetar a mira. Se existe uma opção Raw Input no cliente, registre seu estado e teste separadamente. O laboratório não comprova efeito no jogo.

## Coleta opcional, somente leitura

Baixe `scripts/Diagnosticar-Lunar.ps1`, revise e execute no PowerShell enquanto joga:

```powershell
.\Diagnosticar-Lunar.ps1 -Seconds 15
```

O JSON é salvo na Área de Trabalho do Windows, incluindo redirecionamento para OneDrive. O script apenas consulta CPU, RAM, driver/resolução reportados e processos candidatos Java/Lunar. Não mede FPS, frametime ou rede, não altera configurações e não envia dados. Não coleta command lines ou caminhos. Não exige administrador. Se a política do Windows impedir scripts, não é necessário desativá-la: informe os dados manualmente pelo Gerenciador de Tarefas e as configurações do jogo.

`-SelfTest` valida seis casos de cálculo sem consultar hardware. Isso não substitui execução real do diagnóstico e medições no computador do usuário.
