namespace MissaoBackend.Paginas;

// Política de privacidade em HTML simples, escrita na própria página (sem JavaScript), para que a Google,
// a Apple e quem a abra num navegador a leiam sem depender do site React (que só monta o texto com JS e
// adormece quando está sem visitas). Servida em /privacidade. O texto deve ser igual ao do site
// (MissaoFrontend/src/components/Privacidade/Privacidade.jsx).
public static class PoliticaDePrivacidade
{
    public const string Html = """
<!doctype html>
<html lang="pt-PT">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<meta name="robots" content="index, follow">
<title>Política de Privacidade – Ndatava</title>
<style>
  body { margin: 0; font-family: -apple-system, "Segoe UI", Roboto, Helvetica, Arial, sans-serif; color: #231f20; background: #f8f6f2; line-height: 1.6; }
  header { background: #7a1f2b; color: #fff; padding: 18px 20px; text-align: center; letter-spacing: 4px; font-weight: 700; }
  main { max-width: 760px; margin: 0 auto; padding: 24px 20px 60px; background: #fff; }
  h1 { font-size: 1.7rem; margin: 8px 0 4px; color: #7a1f2b; }
  h2 { font-size: 1.2rem; margin: 28px 0 6px; color: #7a1f2b; }
  p { margin: 8px 0; }
  .data { color: #6b625e; margin-top: 0; margin-bottom: 24px; }
  a { color: #7a1f2b; }
</style>
</head>
<body>
<header>NDATAVA</header>
<main>
<h1>Política de Privacidade</h1>
<p class="data">Última atualização: 10 de novembro de 2026</p>

<p>Esta política explica que dados a aplicação e o site <strong>Ndatava</strong> ("nós", "a aplicação") recolhem, para que servem, e como pode aceder-lhes ou pedir a sua eliminação. Ao usar a Ndatava, aceita esta política.</p>

<h2>1. Que dados recolhemos</h2>
<p><strong>Conta e perfil:</strong> nome, email e palavra-passe (guardada de forma cifrada, nunca em texto simples), e, se preencher, fotografia de perfil, datas de sacramentos (nascimento, batismo, primeira comunhão, crisma, casamento, ordem), diocese e paróquia.</p>
<p><strong>Recuperação da palavra-passe:</strong> se pedir para recuperar a palavra-passe, enviamos para o seu email um código de 6 dígitos, válido durante 15 minutos e de uma só utilização. Guardamos apenas uma versão cifrada do código, que é apagada depois de usada ou de expirar.</p>
<p><strong>Localização:</strong> se autorizar, usamos a localização aproximada do dispositivo apenas para mostrar lojas e artigos mais próximos de si. Não é guardada nem partilhada: é usada no momento do pedido.</p>
<p><strong>Biometria (impressão digital / Face ID):</strong> usada apenas para desbloquear a aplicação no seu próprio dispositivo. Nunca sai do telemóvel nem chega aos nossos servidores.</p>
<p><strong>Microfone e voz (opcional):</strong> nos programas de cânticos da Missa pode gravar a sua voz e/ou converter a sua voz em texto. As gravações e os programas ficam guardados no seu telemóvel e só são enviados para os nossos servidores se você os partilhar com outra pessoa (ver «Partilha de cânticos»). A conversão de voz em texto usa o serviço de reconhecimento de voz do próprio sistema (Google ou Apple), que pode processar o áudio nos servidores deles, segundo as respetivas políticas. Só é usada quando você o pede, e pode retirar a permissão do microfone a qualquer momento nas definições do telemóvel.</p>
<p><strong>Partilha de cânticos (opcional):</strong> para enviar cânticos a outras pessoas dentro da app pode criar uma conta com nome, email e palavra-passe (guardada de forma cifrada). Cada conta tem um código pessoal e pode ser encontrada por outras pessoas pelo nome (e pela paróquia, se a indicar); quem desligar «Aparecer nas pesquisas» só é encontrado pelo código. Para ser contacto é preciso aceitar o pedido, e o email nunca é mostrado a outras pessoas. Só quem tem o seu código ou é seu contacto lhe pode enviar cânticos. Quando partilha um cântico, guardamos nos nossos servidores o título, a letra, a gravação de voz (se a incluir) e o seu nome, para que as pessoas escolhidas o recebam. Cada destinatário pode apagar, denunciar ou bloquear. Os cânticos denunciados são analisados pela nossa equipa. Ao eliminar a conta, apagamos o seu nome, email, código, contactos e todos os cânticos enviados e recebidos. Pode ficar guardada, só durante o tempo necessário para analisar abusos, uma cópia do conteúdo denunciado por outra pessoa. Sem criar conta, nada disto é enviado.</p>
<p><strong>Grupos (opcional):</strong> com conta pode criar ou entrar em grupos (coral, oração, pastoral…). Os grupos não aparecem em pesquisas: só se entra por convite de um administrador ou com o código do grupo, aprovado por um administrador. Os membros veem o nome (e a paróquia, se indicada) dos outros membros e o que é publicado no grupo: avisos, cânticos, orações e programas da Missa, incluindo gravações de voz quando incluídas. Nos avisos, os administradores veem quem já os viu. Cada membro pode sair do grupo, e os administradores podem remover membros e publicações. Qualquer membro pode denunciar uma publicação, que a nossa equipa analisa. Ao sair do grupo ou eliminar a conta, apagamos as suas publicações nesse grupo.</p>
<p><strong>Fotografias:</strong> se optar por carregar uma foto de perfil, ou se for dono de uma loja parceira e carregar fotos de produtos, essas imagens ficam guardadas nos nossos servidores.</p>
<p><strong>Marketplace:</strong> se comprar através da Ndatava, guardamos os dados da encomenda (produtos, valores, contacto) para que a loja parceira a possa preparar e entregar. Se for dono de uma loja, guardamos os dados da loja e dos produtos que publica.</p>

<h2>2. Para que usamos os dados</h2>
<p>Para criar e gerir a sua conta, mostrar-lhe conteúdo relevante (lojas próximas, por exemplo), processar encomendas na loja, e manter a aplicação a funcionar em segurança. Não usamos os seus dados para publicidade, nem os vendemos a terceiros.</p>

<h2>3. Com quem partilhamos dados</h2>
<p>Os dados de uma encomenda (nome, contacto, artigos pedidos) são partilhados apenas com a loja parceira à qual fez a encomenda, para que a possa preparar. Não partilhamos os seus dados com mais ninguém, exceto se exigido por lei.</p>

<h2>4. Onde ficam guardados os dados</h2>
<p>Os dados ficam numa base de dados alojada por um fornecedor de serviços cloud, com ligação cifrada (HTTPS/TLS) entre a aplicação e os nossos servidores.</p>

<h2>5. Os seus direitos e a eliminação da conta</h2>
<p>Pode, a qualquer momento, editar o seu perfil dentro da aplicação. Pode também <strong>eliminar a sua conta e os dados associados diretamente na aplicação</strong>: em <em>Eu → Partilhas → Conta → Eliminar conta</em> (para contas de utilizador) ou na área da loja, em <em>Eliminar conta e dados</em>. A eliminação é imediata. Se preferir, pode pedir-nos a eliminação escrevendo para o email abaixo; respondemos em até 30 dias.</p>

<h2>6. Crianças</h2>
<p>A Ndatava não se dirige especificamente a crianças menores de 13 anos e não recolhe intencionalmente dados de crianças nessa faixa etária.</p>

<h2>7. Alterações a esta política</h2>
<p>Podemos atualizar esta política ocasionalmente. A data no topo desta página indica a versão mais recente.</p>

<h2>8. Contacto</h2>
<p>Para questões sobre privacidade ou para pedir a eliminação dos seus dados, escreva para <a href="mailto:ndatava3@gmail.com">ndatava3@gmail.com</a>.</p>
</main>
</body>
</html>
""";
}
