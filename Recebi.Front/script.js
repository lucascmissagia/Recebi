/* ===========================================================
   RECEBI – Funções de API e Sessão (Global)
   =========================================================== */

const API_BASE_URL = 'https://localhost:7174/api'; 
const TOKEN_KEY = 'recebi_token';
const USER_KEY = 'recebi_user';
const ITEMS_PER_PAGE = 10; 


/* =========================================
      FUNÇÕES DE SESSÃO E LOGOUT 
   ========================================= */

function fazerLogout() {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
  window.location.href = '../home.html';
}

function setupHeaderSessao() {
  const userPillBtn = document.getElementById('user-pill-btn');
  const logoutMenu = document.getElementById('logout-menu');
  const btnLogout = document.getElementById('btn-logout');
  const userPillDiv = document.getElementById('user-pill'); 

  let user;
  try {
    user = JSON.parse(localStorage.getItem(USER_KEY));
  } catch (e) { user = null; }

  if (!user || !user.nome) {
    console.warn('Nenhum usuário logado. Redirecionando para login.');
    setTimeout(fazerLogout, 300);
    if (userPillBtn) userPillBtn.style.display = 'none';
    if (userPillDiv) userPillDiv.style.display = 'none';
    return;
  }

  const nomeTarget = userPillBtn ? userPillBtn.querySelector('span') : (userPillDiv || null);
  if (nomeTarget) {
      nomeTarget.textContent = user.nome;
  }
  
  if (userPillBtn && logoutMenu && btnLogout) {
    userPillBtn.addEventListener('click', (e) => {
      e.stopPropagation();
      logoutMenu.classList.toggle('show');
      userPillBtn.classList.toggle('open');
    });
    btnLogout.addEventListener('click', (e) => {
      e.preventDefault();
      fazerLogout();
    });
    window.addEventListener('click', (e) => { 
      if (logoutMenu.classList.contains('show') && !userPillBtn.contains(e.target)) {
        logoutMenu.classList.remove('show');
        userPillBtn.classList.remove('open');
      }
    });
  } else {
     const btnLogoutStandalone = document.getElementById('btn-logout');
     if(btnLogoutStandalone) {
        btnLogoutStandalone.addEventListener('click', fazerLogout);
     }
     if(userPillDiv) {
         userPillDiv.style.cursor = 'pointer';
         userPillDiv.title = 'Clique para Sair';
         userPillDiv.addEventListener('click', fazerLogout);
     }
  }
}


/* =========================================
       HELPER MESTRE DA API 
   ========================================= */

async function fetchApi(endpoint, options = {}) {
  const token = localStorage.getItem(TOKEN_KEY);
  const headers = new Headers(options.headers || {});
  headers.append('Content-Type', 'application/json');
  if (token) {
    headers.append('Authorization', 'Bearer ' + token);
  } else if (!endpoint.endsWith('/Usuario/login')) {
      console.warn('Token não encontrado. Redirecionando para login.');
      fazerLogout();
      return Promise.reject(new Error('Token não encontrado'));
  }

  try {
    const response = await fetch(API_BASE_URL + endpoint, { ...options, headers: headers });
    if (response.status === 401) { 
      alert('Sua sessão expirou ou é inválida. Por favor, faça login novamente.');
      fazerLogout();
      return Promise.reject(new Error('Não autorizado'));
    }
    if (response.status === 403) { 
        return response; 
    }
    return response;
  } catch (err) {
    if (err.message.includes('Failed to fetch')) {
      alert('Erro de conexão: Não foi possível se conectar à API. O back-end está rodando?');
    }
    return Promise.reject(err);
  }
}

/* =========================================
      FUNÇÕES AUXILIARES GERAIS
   ========================================= */
function formatarData(dataISO) {
    if (!dataISO) return '-';
    try {
        return new Date(dataISO).toLocaleDateString('pt-BR');
    } catch {
        return '-';
    }
}
function formatarDataHora(dataISO) {
    if (!dataISO) return '-';
    try {
        return new Date(dataISO).toLocaleString('pt-BR');
    } catch {
        return '-';
    }
}
function formatarDetalhes(detalhesJsonString) {
    if (!detalhesJsonString) return 'Nenhum detalhe disponível.';
    try {
        const detalhes = JSON.parse(detalhesJsonString);
        let output = '';
        for (const key in detalhes) {
            if (Object.hasOwnProperty.call(detalhes, key)) {
                const value = detalhes[key];
                if (typeof value === 'object' && value !== null && 'Old' in value && 'New' in value) {
                    output += `<strong>${key}:</strong> alterado de <i>'${value.Old || '(vazio)'}'</i> para <i>'${value.New || '(vazio)'}'</i><br>`;
                } else if (typeof value === 'object' && value !== null && 'Info' in value) {
                     output += `<strong>${key}:</strong> ${value.Info}<br>`;
                } else {
                    output += `<strong>${key}:</strong> '${value || '(vazio)'}'<br>`;
                }
            }
        }
        return output || 'Nenhum detalhe específico registado.';
    } catch (e) {
        console.error('Erro ao parsear detalhes JSON:', e);
        return 'Erro ao formatar detalhes.';
    }
}
function escapeHtml(unsafe) {
    if (!unsafe) return '';
    return unsafe
         .replace(/&/g, '&amp;')
         .replace(/</g, '&lt;')
         .replace(/>/g, '&gt;')
         .replace(/"/g, '&quot;')
         .replace(/'/g, '&#039;');
} 


/* =========================================
      FUNÇÕES DE RENDERIZAÇÃO (PORTEIRO)
   ========================================= */

// --- Renderizador para Porteiro (index.html, entregas.html) ---
async function renderEntregas({ endpoint, destinoTabela, filtroInput, limite }) {
  const tbody = document.querySelector(destinoTabela);
  if (!tbody) return;

  const theadRow = tbody.closest('table')?.querySelector('thead tr');
  if (theadRow && !theadRow.querySelector('.col-acoes')) {
    const thAcoes = document.createElement('th');
    thAcoes.textContent = 'Ações';
    thAcoes.classList.add('col-acoes');
    theadRow.appendChild(thAcoes);
  }

  const input = filtroInput ? document.querySelector(filtroInput) : null;
  
  const firstPageBtn = document.getElementById('firstPageBtn');
  const prevPageBtn = document.getElementById('prevPageBtn');
  const pageInfoSpan = document.getElementById('pageInfo');
  const nextPageBtn = document.getElementById('nextPageBtn');
  const lastPageBtn = document.getElementById('lastPageBtn');

  let currentPage = 1;
  let todasEncomendas = [];      
  let filteredEncomendas = [];   
  let totalPages = 1;
  const hasPagination = firstPageBtn && pageInfoSpan; 
  const itemsToRender = hasPagination ? ITEMS_PER_PAGE : (limite || undefined); 

  function drawCurrentPage() {
      let pageData;

      if (hasPagination) {
          const startIndex = (currentPage - 1) * ITEMS_PER_PAGE;
          const endIndex = startIndex + ITEMS_PER_PAGE;
          pageData = filteredEncomendas.slice(startIndex, endIndex); 
      } else {
          pageData = filteredEncomendas.slice(0, itemsToRender);
      }
      
      if (pageData.length === 0 && filteredEncomendas.length > 0) {
          tbody.innerHTML = `<tr><td colspan="8">Nenhuma encomenda encontrada nesta página.</td></tr>`;
      } else if (pageData.length === 0) {
          tbody.innerHTML = `<tr><td colspan="8">Nenhuma encomenda encontrada.</td></tr>`;
      } else {
          tbody.innerHTML = pageData.map(d => `
              <tr>
                <td>${d.idEncomenda}</td>
                <td>${d.morador || 'N/A'}</td>
                <td>${d.apartamento}</td>
                <td>${formatarData(d.dataEntrada)}</td>
                <td>${formatarData(d.dataRetirada)}</td>
                <td>${d.porteiro || 'N/A'}</td>
                <td>${d.codigoRastreio || '-'}</td>
                <td class="actions-column"> 
                  <a class="btn-link" href="detalhes-encomenda.html?id=${d.idEncomenda}">Detalhes</a>
                </td>
              </tr>
            `).join('');
      }

      // Atualiza controles de paginação (se existirem)
      if (hasPagination) {
          totalPages = Math.ceil(filteredEncomendas.length / ITEMS_PER_PAGE) || 1;
          if (pageInfoSpan) pageInfoSpan.textContent = `Página ${currentPage} de ${totalPages}`;
          const onFirstPage = (currentPage === 1);
          const onLastPage = (currentPage === totalPages);
          if (firstPageBtn) firstPageBtn.disabled = onFirstPage;
          if (prevPageBtn) prevPageBtn.disabled = onFirstPage;
          if (nextPageBtn) nextPageBtn.disabled = onLastPage;
          if (lastPageBtn) lastPageBtn.disabled = onLastPage;
      }
  }

  function filterAndDraw(filtro) {
      const q = filtro ? filtro.toLowerCase().trim() : '';
      
      filteredEncomendas = q ? todasEncomendas.filter(d =>
          (d.idEncomenda?.toString() || '').includes(q) ||
          (d.morador || '').toLowerCase().includes(q) ||    
          (d.apartamento || '').toLowerCase().includes(q) || 
          (d.codigoRastreio || '').toLowerCase().includes(q)
       ) : [...todasEncomendas]; 

      currentPage = 1; 
      drawCurrentPage(); 
  }

  // Função para buscar dados da API
  async function fetchAndSetup() {
      try {
        tbody.innerHTML = '<tr><td colspan="8">Carregando...</td></tr>';
        const response = await fetchApi(endpoint); 
        if (!response.ok) {
          const errJson = await response.json().catch(() => ({}));
          throw new Error(errJson.message || 'Falha ao buscar dados');
        }
        
        const dados = await response.json();

        if (dados.message || !Array.isArray(dados)) { 
          todasEncomendas = []; 
        } else {
          todasEncomendas = dados; 
        }
        
        filterAndDraw(input ? input.value : '');
        
      } catch (err) {
        todasEncomendas = []; 
        filteredEncomendas = [];
        tbody.innerHTML = `<tr><td colspan="8">Erro ao carregar dados: ${err.message}</td></tr>`;
        
        if (hasPagination) {
            if (pageInfoSpan) pageInfoSpan.textContent = `Erro`;
            [firstPageBtn, prevPageBtn, nextPageBtn, lastPageBtn].forEach(btn => { if(btn) btn.disabled = true; });
        }
      }
  }

  if (input && !input.dataset.listenerAttached) {
      input.addEventListener('input', () => {
          filterAndDraw(input.value); 
      });
      input.dataset.listenerAttached = 'true'; 
  }

  [
      { btn: firstPageBtn, action: () => currentPage = 1 },
      { btn: prevPageBtn, action: () => { if (currentPage > 1) currentPage--; } },
      { btn: nextPageBtn, action: () => { if (currentPage < totalPages) currentPage++; } },
      { btn: lastPageBtn, action: () => currentPage = totalPages }
  ].forEach(({ btn, action }) => {
      if (btn && !btn.dataset.listenerAttached) {
          btn.addEventListener('click', () => { action(); drawCurrentPage(); });
          btn.dataset.listenerAttached = 'true';
      }
  });

  await fetchAndSetup();
}

/* =========================================
      FUNÇÕES DE RENDERIZAÇÃO (MORADOR)
   ========================================= */

// --- Renderizador para Morador (index.html) ---
async function renderMinhasEncomendas({ status, destinoTabela, filtroInput }) {
    const tbody = document.querySelector(destinoTabela);
    if (!tbody) return;
    
    const input = filtroInput ? document.querySelector(filtroInput) : null;
    
    const firstPageBtn = document.getElementById('firstPageBtn');
    const prevPageBtn = document.getElementById('prevPageBtn');
    const pageInfoSpan = document.getElementById('pageInfo');
    const nextPageBtn = document.getElementById('nextPageBtn');
    const lastPageBtn = document.getElementById('lastPageBtn');

    let currentPage = 1;
    let todasEncomendas = [];      
    let filteredEncomendas = [];   
    let totalPages = 1;

    function drawCurrentPage() {
        const startIndex = (currentPage - 1) * ITEMS_PER_PAGE;
        const endIndex = startIndex + ITEMS_PER_PAGE;
        const pageData = filteredEncomendas.slice(startIndex, endIndex);

        if (pageData.length === 0 && filteredEncomendas.length > 0) {
            tbody.innerHTML = `<tr><td colspan="5">Nenhuma encomenda encontrada nesta página.</td></tr>`;
        } else if (pageData.length === 0) {
            tbody.innerHTML = `<tr><td colspan="5">Nenhuma encomenda encontrada.</td></tr>`;
        } else {
            tbody.innerHTML = pageData.map(d => `
              <tr>
                <td>${d.codigoRastreio || d.idEncomenda || '-'}</td> 
                <td>${formatarData(d.dataEntrada)}</td>
                <td>${d.dataRetirada ? formatarData(d.dataRetirada) : 'Pendente'}</td>
                <td>${d.porteiro || 'N/A'}</td> 
                <td class="actions-column"> <a class="btn-link" href="detalhes.html?id=${d.idEncomenda}">Ver detalhes</a>
                </td>
              </tr>
            `).join('');
        }

        totalPages = Math.ceil(filteredEncomendas.length / ITEMS_PER_PAGE) || 1;
        if (pageInfoSpan) pageInfoSpan.textContent = `Página ${currentPage} de ${totalPages}`;
        
        const onFirstPage = (currentPage === 1);
        const onLastPage = (currentPage === totalPages);

        if (firstPageBtn) firstPageBtn.disabled = onFirstPage;
        if (prevPageBtn) prevPageBtn.disabled = onFirstPage;
        if (nextPageBtn) nextPageBtn.disabled = onLastPage;
        if (lastPageBtn) lastPageBtn.disabled = onLastPage;
    }

    function filterAndDraw(filtro) {
        const q = filtro ? filtro.toLowerCase().trim() : '';
        
        filteredEncomendas = q ? todasEncomendas.filter(d =>
            (d.codigoRastreio || d.idEncomenda?.toString() || '').toLowerCase().includes(q) ||
            (d.porteiro || '').toLowerCase().includes(q) || 
            (formatarData(d.dataEntrada)).includes(q) ||
            (formatarData(d.dataRetirada)).includes(q)
         ) : [...todasEncomendas]; 

        currentPage = 1; 
        drawCurrentPage(); 
    }

    // Busca dados da API
    async function fetchAndSetup() {
        let endpoint = '/Morador/encomendas'; 
        if (status && status !== 'todas') {
            endpoint += `?status=${encodeURIComponent(status)}`; 
        }

        try {
          tbody.innerHTML = '<tr><td colspan="5">Carregando suas encomendas...</td></tr>'; 
          const response = await fetchApi(endpoint); 

          if (!response.ok) {
            const errJson = await response.json().catch(() => ({}));
            throw new Error(errJson.message || 'Falha ao buscar suas encomendas');
          }
          
          const dados = await response.json();

          if (dados.message || !Array.isArray(dados)) { 
            todasEncomendas = []; 
          } else {
            todasEncomendas = dados; 
          }
          
          filterAndDraw(input ? input.value : '');
          
        } catch (err) {
          todasEncomendas = []; 
          filteredEncomendas = [];
          tbody.innerHTML = `<tr><td colspan="5">Erro ao carregar dados: ${err.message}</td></tr>`;
          
          if (pageInfoSpan) pageInfoSpan.textContent = `Erro`;
          [firstPageBtn, prevPageBtn, nextPageBtn, lastPageBtn].forEach(btn => { if(btn) btn.disabled = true; });
        }
    }

    if (input && !input.dataset.listenerAttached) {
        input.addEventListener('input', () => {
            filterAndDraw(input.value);
        });
        input.dataset.listenerAttached = 'true';
    }

    [
        { btn: firstPageBtn, action: () => currentPage = 1 },
        { btn: prevPageBtn, action: () => { if (currentPage > 1) currentPage--; } },
        { btn: nextPageBtn, action: () => { if (currentPage < totalPages) currentPage++; } },
        { btn: lastPageBtn, action: () => currentPage = totalPages }
    ].forEach(({ btn, action }) => {
        if (btn && !btn.dataset.listenerAttached) {
            btn.addEventListener('click', () => { action(); drawCurrentPage(); });
            btn.dataset.listenerAttached = 'true';
        }
    });

    await fetchAndSetup();
}

/* =========================================
      FUNÇÕES DE RENDERIZAÇÃO (ADMIN/SINDICO)
   ========================================= */

// --- Renderizador de Usuários (COMPLETO para usuarios.html) ---
async function renderUsuarios({ destinoTabela, filtroInput, pageInfoId, prevBtnId, nextBtnId, firstBtnId, lastBtnId, showInactiveCheckboxId }) {
    const tbody = document.querySelector(destinoTabela);
    if (!tbody) return console.error('Elemento tbody não encontrado:', destinoTabela);

    const input = filtroInput ? document.querySelector(filtroInput) : null;
    const showInactiveCheckbox = document.getElementById(showInactiveCheckboxId); 
    const pageInfoSpan = document.getElementById(pageInfoId);
    const firstPageBtn = document.getElementById(firstBtnId);
    const prevPageBtn = document.getElementById(prevBtnId);
    const nextPageBtn = document.getElementById(nextBtnId);
    const lastPageBtn = document.getElementById(lastBtnId);
    const COLUMNS = 6; 
    let currentPage = 1;
    let todosUsuarios = [];
    let filteredUsuarios = [];
    let totalPages = 1;

    function drawCurrentPage() {
        const startIndex = (currentPage - 1) * ITEMS_PER_PAGE;
        const endIndex = startIndex + ITEMS_PER_PAGE;
        const pageData = filteredUsuarios.slice(startIndex, endIndex);

        if (pageData.length === 0 && filteredUsuarios.length > 0) {
           tbody.innerHTML = `<tr><td colspan="${COLUMNS}">Nenhum usuário encontrado nesta página.</td></tr>`;
        } else if (pageData.length === 0) {
            tbody.innerHTML = `<tr><td colspan="${COLUMNS}">Nenhum usuário encontrado.</td></tr>`;
        } else {
            tbody.innerHTML = pageData.map(u => `
              <tr>
                <td>${u.idUsuario}</td>
                <td>${u.nome}</td>
                <td>${u.apartamento || '-'}</td>
                <td>${u.tipoUsuario}</td>
                <td>${u.status}</td> 
                <td class="actions-column">
                  <button class="btn-link edit-user" data-id="${u.idUsuario}">Editar</button>
                </td>
              </tr>
            `).join('');
            addTableActionListeners(); 
        }

        totalPages = Math.ceil(filteredUsuarios.length / ITEMS_PER_PAGE) || 1;
        if (pageInfoSpan) pageInfoSpan.textContent = `Página ${currentPage} de ${totalPages}`;
        const onFirst = currentPage === 1;
        const onLast = currentPage === totalPages;
        if (firstPageBtn) firstPageBtn.disabled = onFirst;
        if (prevPageBtn) prevPageBtn.disabled = onFirst;
        if (nextPageBtn) nextPageBtn.disabled = onLast;
        if (lastPageBtn) lastPageBtn.disabled = onLast;
    }

    function filterAndDraw(filtroTexto) {
         const q = filtroTexto ? filtroTexto.toLowerCase().trim() : '';
         let baseList = showInactiveCheckbox?.checked ? todosUsuarios : todosUsuarios.filter(u => u.status === 'Ativo');
         
         filteredUsuarios = q ? baseList.filter(u =>
             u.idUsuario.toString().includes(q) ||
             (u.nome || '').toLowerCase().includes(q) ||
             (u.apartamento || '').toLowerCase().includes(q) ||
             (u.tipoUsuario || '').toLowerCase().includes(q) ||
             (u.email || '').toLowerCase().includes(q) ||
             (u.status || '').toLowerCase().includes(q) 
          ) : [...baseList];
          
         currentPage = 1;
         drawCurrentPage();
    }

    // Busca dados da API (SEMPRE BUSCA TODOS)
    async function fetchAndSetup() {
        try {
            tbody.innerHTML = `<tr><td colspan="${COLUMNS}">Carregando usuários...</td></tr>`; 
            const response = await fetchApi('/Sindico/usuarios?status=todos'); 
            if (!response.ok) {
                const errJson = await response.json().catch(() => ({}));
                throw new Error(errJson.message || 'Falha ao buscar usuários');
            }
            const dados = await response.json();
            todosUsuarios = dados.message ? [] : (Array.isArray(dados) ? dados : []);
            filterAndDraw(input ? input.value : ''); 
        } catch (err) {
            tbody.innerHTML = `<tr><td colspan="${COLUMNS}">Erro: ${err.message}</td></tr>`;
            if (pageInfoSpan) pageInfoSpan.textContent = `Erro`;
            [firstPageBtn, prevPageBtn, nextPageBtn, lastPageBtn].forEach(btn => { if(btn) btn.disabled = true; });
        }
    }

    if (input && !input.dataset.listenerAttached) {
        input.addEventListener('input', () => filterAndDraw(input.value)); 
        input.dataset.listenerAttached = 'true';
    }
    if (showInactiveCheckbox && !showInactiveCheckbox.dataset.listenerAttached) { 
        showInactiveCheckbox.addEventListener('change', () => filterAndDraw(input ? input.value : '')); 
        showInactiveCheckbox.dataset.listenerAttached = 'true';
    }

    [
        { btn: firstPageBtn, action: () => currentPage = 1 },
        { btn: prevPageBtn, action: () => { if (currentPage > 1) currentPage--; } },
        { btn: nextPageBtn, action: () => { if (currentPage < totalPages) currentPage++; } },
        { btn: lastPageBtn, action: () => currentPage = totalPages }
    ].forEach(({ btn, action }) => {
        if (btn && !btn.dataset.listenerAttached) {
            btn.addEventListener('click', () => { action(); drawCurrentPage(); });
            btn.dataset.listenerAttached = 'true';
        }
    });

    function addTableActionListeners() {
        tbody.querySelectorAll('.edit-user').forEach(button => {
            button.replaceWith(button.cloneNode(true)); 
        });
        tbody.querySelectorAll('.edit-user').forEach(button => {
             button.addEventListener('click', (e) => {
                 const userId = e.target.dataset.id;
                 window.location.href = `editar-user.html?id=${userId}`;
             });
        });
    }

    await fetchAndSetup();
}

async function renderUsuariosResumo({ destinoTabela, limit = 10 }) {
    const tbody = document.querySelector(destinoTabela);
    if (!tbody) return console.error('Elemento tbody não encontrado:', destinoTabela);
    
    try {
        tbody.innerHTML = `<tr><td colspan="4">Carregando usuários...</td></tr>`; 
        const response = await fetchApi('/Sindico/usuarios?status=Ativo'); 
        if (!response.ok) {
            const errJson = await response.json().catch(() => ({}));
            throw new Error(errJson.message || 'Falha ao buscar usuários');
        }
        const dados = await response.json();
        
        if (dados.message || !Array.isArray(dados) || dados.length === 0) {
             tbody.innerHTML = `<tr><td colspan="4">${dados.message || 'Nenhum usuário ativo encontrado.'}</td></tr>`;
             return;
        }

        const dadosResumo = dados.slice(0, limit);

        tbody.innerHTML = dadosResumo.map(u => `
          <tr>
            <td>${u.idUsuario}</td>
            <td>${u.nome}</td>
            <td>${u.apartamento || '-'}</td>
            <td>${u.tipoUsuario}</td>
          </tr>
        `).join('');

    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="4">Erro: ${err.message}</td></tr>`;
    }
}

// Resumo de entregas (admin/index.html)
async function renderEncomendasResumo({ destinoTabela, limit = 10 }) {
    const tbody = document.querySelector(destinoTabela);
    if (!tbody) return console.error('Elemento tbody não encontrado:', destinoTabela);
    
    try {
        tbody.innerHTML = `<tr><td colspan="4">Carregando entregas...</td></tr>`; 
        const response = await fetchApi('/Encomenda/todas'); 
        if (!response.ok) {
            const errJson = await response.json().catch(() => ({}));
            throw new Error(errJson.message || 'Falha ao buscar encomendas');
        }
        const dados = await response.json();
        
        if (dados.message || !Array.isArray(dados) || dados.length === 0) {
             tbody.innerHTML = `<tr><td colspan="4">${dados.message || 'Nenhuma encomenda encontrada.'}</td></tr>`;
             return;
        }

        const dadosResumo = dados.slice(0, limit);

        tbody.innerHTML = dadosResumo.map(d => `
          <tr>
            <td>${d.idEncomenda}</td>
            <td>${d.morador || 'N/A'}</td>
            <td>${formatarData(d.dataEntrada)}</td>
            <td>${d.status}</td>
          </tr>
        `).join('');

    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="4">Erro: ${err.message}</td></tr>`;
    }
}

// Buscar detalhes de uma encomenda por ID (usado em admin/detalhes-encomenda.html)
async function buscarDetalhesEncomendaPorId(id) {
  try {
    if (!id) throw new Error('ID inválido');
    const resp = await fetchApi(`/Encomenda/${encodeURIComponent(id)}`);
    if (!resp.ok) {
      const errJson = await resp.json().catch(() => ({}));
      throw new Error(errJson.message || `Falha ao buscar encomenda (Status: ${resp.status})`);
    }
    const dados = await resp.json();
    return {
      idEncomenda: dados.idEncomenda ?? dados.id ?? id,
      status: dados.status || '-',
      morador: dados.morador || dados.nomeMorador || 'N/A',
      apartamento: dados.apartamento || dados.apart || '-',
      dataEntrada: dados.dataEntrada || dados.dataRecebimento || null,
      dataRetirada: dados.dataRetirada || null,
      codigoRastreio: dados.codigoRastreio || dados.codRastreio || '-',
      porteiro: dados.porteiro || dados.nomePorteiro || 'N/A',
      descricao: dados.descricao || '-'
    };
  } catch (err) {
    console.error('Erro ao buscar detalhes da encomenda:', err);
    return null;
  }
}

// Buscar detalhes de um log específico (detalhes-log.html)
async function buscarDetalhesLogPorId(id) {
  try {
    if (!id) throw new Error('ID inválido');
    const resp = await fetchApi(`/Sindico/logs/${encodeURIComponent(id)}`);
    if (!resp.ok) {
      const errJson = await resp.json().catch(() => ({}));
      throw new Error(errJson.message || `Falha ao buscar log (Status: ${resp.status})`);
    }
    const dados = await resp.json();
    return {
      idHistorico: dados.idHistorico ?? dados.id ?? id,
      usuario: dados.usuario ?? dados.nomeUsuario ?? 'N/A',
      idUsuario: dados.idUsuario ?? null,
      acao: dados.acao ?? '-',
      tipo: dados.tipo ?? '-',
      dataHora: dados.dataHora ?? null,
      idEncomenda: dados.idEncomenda ?? null,
      encomendaApartamento: dados.encomendaApartamento ?? '-',
      detalhes: typeof dados.detalhes === 'string' ? dados.detalhes : JSON.stringify(dados.detalhes || '')
    };
  } catch (err) {
    console.error('Erro ao buscar detalhes do log:', err);
    return null;
  }
}


// --- Funções CRUD e Detalhes (Admin) ---
async function criarNovoUsuario(userData) {
    try {
        const response = await fetchApi('/Sindico/criar', {
            method: 'POST',
            body: JSON.stringify(userData)
        });
        if (!response.ok) {
            const errJson = await response.json().catch(() => ({}));
            throw new Error(errJson.message || `Falha ao criar usuário (Status: ${response.status})`);
        }
        const data = await response.json();
        alert(data.message || 'Usuário criado!');
        window.location.href = 'usuarios.html';
    } catch (err) {
        alert('Erro: ' + err.message);
    }
}

async function buscarUsuarioPorId(id) {
    try {
        const response = await fetchApi(`/Sindico/usuarios/${id}`);
        if (!response.ok) {
            const errJson = await response.json().catch(() => ({}));
            throw new Error(errJson.message || `Falha ao buscar usuário (Status: ${response.status})`);
        }
        return await response.json();
    } catch (err) {
        alert('Erro ao buscar dados do usuário: ' + err.message);
        return null; 
    }
}

async function atualizarUsuario(id, userData) {
     try {
        delete userData.tipoUsuario; 
        
        const response = await fetchApi(`/Sindico/atualizar/${id}`, {
            method: 'PUT',
            body: JSON.stringify(userData) 
        });
        if (!response.ok) {
           const errJson = await response.json().catch(() => ({ message: 'Resposta inválida da API', error: `Status ${response.status}` }));
           const error = new Error(errJson.message || `Falha ao atualizar usuário (Status: ${response.status})`);
           error.apiError = errJson.error; throw error; 
        }
        const data = await response.json();
        alert(data.message || 'Usuário atualizado!');
        window.location.href = 'usuarios.html';
    } catch (err) {
        let detailedError = err.apiError || err.message || 'Erro desconhecido';
        alert('Erro ao atualizar: ' + detailedError);
    }
}

// --- Renderizador de Encomendas (COMPLETO para admin/encomendas.html) ---
async function renderTodasEncomendas({ destinoTabela, filtroInput, pageInfoId, prevBtnId, nextBtnId, firstBtnId, lastBtnId, dataInicioInput, dataFimInput, exportBtnId }) {
    const tbody = document.querySelector(destinoTabela);
    if (!tbody) return console.error('Elemento tbody não encontrado:', destinoTabela);

    const COLUMNS = 8; 

    const input = filtroInput ? document.querySelector(filtroInput) : null;
    const dataInicioEl = dataInicioInput ? document.querySelector(dataInicioInput) : null;
    const dataFimEl = dataFimInput ? document.querySelector(dataFimInput) : null;
    const pageInfoSpan = document.getElementById(pageInfoId);
    const firstPageBtn = document.getElementById(firstBtnId);
    const prevPageBtn = document.getElementById(prevBtnId);
    const nextPageBtn = document.getElementById(nextBtnId);
    const lastPageBtn = document.getElementById(lastBtnId);
    const exportBtn = document.getElementById(exportBtnId);
    
    let currentPage = 1;
    let todasEncomendas = [];
    let filteredEncomendas = [];
    let totalPages = 1;

    if (input && !input.dataset.listenerAttached) {
    input.addEventListener('input', () => filterAndDraw());
    input.dataset.listenerAttached = 'true';
    }

    const onDateChange = () => filterAndDraw();
    [dataInicioEl, dataFimEl].forEach((el) => {
    if (el && !el.dataset.listenerAttached) {
        el.addEventListener('change', onDateChange);
        el.addEventListener('input', onDateChange);
        el.dataset.listenerAttached = 'true';
    }
    });


    function drawCurrentPage() {
        const startIndex = (currentPage - 1) * ITEMS_PER_PAGE;
        const endIndex = startIndex + ITEMS_PER_PAGE;
        const pageData = filteredEncomendas.slice(startIndex, endIndex);

        if (pageData.length === 0 && filteredEncomendas.length > 0) {
           tbody.innerHTML = `<tr><td colspan="${COLUMNS}">Nenhuma encomenda encontrada nesta página.</td></tr>`;
        } else if (pageData.length === 0) {
            tbody.innerHTML = `<tr><td colspan="${COLUMNS}">Nenhuma encomenda encontrada.</td></tr>`;
        } else {
            tbody.innerHTML = pageData.map(d => `
              <tr>
                <td>${d.idEncomenda}</td>
                <td>${d.morador || 'N/A'}</td>
                <td>${d.apartamento}</td>
                <td>${formatarData(d.dataEntrada)}</td>
                <td>${formatarData(d.dataRetirada)}</td>
                <td>${d.porteiro || 'N/A'}</td>
                <td>${d.codigoRastreio || '-'}</td>
                <td class="actions-column">
                  <a class="btn-link" href="detalhes-encomenda.html?id=${d.idEncomenda}">Detalhes</a>
                  </td>
              </tr>
            `).join('');
        }

        totalPages = Math.ceil(filteredEncomendas.length / ITEMS_PER_PAGE) || 1;
        if (pageInfoSpan) pageInfoSpan.textContent = `Página ${currentPage} de ${totalPages}`;
        const onFirst = currentPage === 1;
        const onLast = currentPage === totalPages;
        if (firstPageBtn) firstPageBtn.disabled = onFirst;
        if (prevPageBtn) prevPageBtn.disabled = onFirst;
        if (nextPageBtn) nextPageBtn.disabled = onLast;
        if (lastPageBtn) lastPageBtn.disabled = onLast;
    }

    function filterAndDraw() {
        const q = input ? input.value.toLowerCase().trim() : '';
        const dataInicio = dataInicioEl ? dataInicioEl.value : null;
        const dataFim = dataFimEl ? dataFimEl.value : null;

        const inicioTimestamp = dataInicio ? new Date(dataInicio + 'T00:00:00').getTime() : 0;
        const fimTimestamp = dataFim ? new Date(dataFim + 'T23:59:59').getTime() : Infinity;

        filteredEncomendas = todasEncomendas.filter(d => {
            const matchTexto = !q || (
                d.idEncomenda.toString().includes(q) ||
                (d.morador || '').toLowerCase().includes(q) ||
                (d.apartamento || '').toLowerCase().includes(q) ||
                (d.porteiro || '').toLowerCase().includes(q) ||
                (d.codigoRastreio || '').toLowerCase().includes(q)
            );
            if (!matchTexto) return false;

            const dataEntradaTs = d.dataEntrada ? new Date(d.dataEntrada).getTime() : 0;
            const matchData = dataEntradaTs >= inicioTimestamp && dataEntradaTs <= fimTimestamp;

            return matchData;
        });

        currentPage = 1;
        drawCurrentPage();
    }

    // Busca dados da API
    async function fetchAndSetup() {
        try {
            tbody.innerHTML = `<tr><td colspan="${COLUMNS}">Carregando encomendas...</td></tr>`;
            const response = await fetchApi('/Encomenda/todas');
            if (!response.ok) {
                const errJson = await response.json().catch(() => ({}));
                throw new Error(errJson.message || `Falha ao buscar encomendas (Status: ${response.status})`);
            }
            const dados = await response.json();
            todasEncomendas = dados.message ? [] : (Array.isArray(dados) ? dados : []);
            filterAndDraw(); 
        } catch (err) {
            tbody.innerHTML = `<tr><td colspan="${COLUMNS}">Erro: ${err.message || err}</td></tr>`;
            if (pageInfoSpan) pageInfoSpan.textContent = `Erro`;
            [firstPageBtn, prevPageBtn, nextPageBtn, lastPageBtn].forEach(btn => { if(btn) btn.disabled = true; });
        }
    }

    if (exportBtn && !exportBtn.dataset.listenerAttached) {
      exportBtn.addEventListener('click', () => {
        const linhas = (filteredEncomendas && filteredEncomendas.length)
          ? filteredEncomendas
          : todasEncomendas;

        if (!linhas || linhas.length === 0) {
          alert('Não há dados para exportar.');
          return;
        }

        const header = [
          'ID','Morador','Apartamento','Data Recebimento',
          'Data Retirada','Porteiro','Cod. Rastreio','Status'
        ];

        const csvRows = [];
        csvRows.push(header.join(';'));

        for (const d of linhas) {
          const row = [
            d.idEncomenda ?? d.id ?? '',
            d.morador ?? '',
            d.apartamento ?? '',
            d.dataEntrada ? formatarData(d.dataEntrada) : '',
            d.dataRetirada ? formatarData(d.dataRetirada) : '',
            d.porteiro ?? '',
            d.codigoRastreio ?? '',
            d.status ?? ''
          ].map(v => `"${String(v).replace(/"/g, '""')}"`);
          csvRows.push(row.join(';'));
        }

        const csv = csvRows.join('\n');
        const BOM = '\uFEFF'; 
        const blob = new Blob([BOM + csv], { type: 'text/csv;charset=utf-8;' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        const ts = new Date().toISOString().slice(0,19).replace(/[:T]/g,'-');
        a.href = url;
        a.download = `encomendas_${ts}.csv`;
        document.body.appendChild(a);
        a.click();
        a.remove();
        URL.revokeObjectURL(url);
      });
      exportBtn.dataset.listenerAttached = 'true';
    }

    [
        { btn: firstPageBtn, action: () => currentPage = 1 },
        { btn: prevPageBtn, action: () => { if (currentPage > 1) currentPage--; } },
        { btn: nextPageBtn, action: () => { if (currentPage < totalPages) currentPage++; } },
        { btn: lastPageBtn, action: () => currentPage = totalPages }
    ].forEach(({ btn, action }) => {
        if (btn && !btn.dataset.listenerAttached) {
            btn.addEventListener('click', () => { action(); drawCurrentPage(); });
            btn.dataset.listenerAttached = 'true';
        }
    });

    await fetchAndSetup();
}

// --- Renderizador de Logs (COMPLETO para Registros.html) ---
async function renderLogs({ destinoTabela, pageInfoId, prevBtnId, nextBtnId, firstBtnId, lastBtnId, exportBtnId }) {
    const tbody = document.querySelector(destinoTabela);
    if (!tbody) return console.error('Elemento tbody não encontrado:', destinoTabela);
    
    const COLUMNS = 7; 
    const pageInfoSpan = document.getElementById(pageInfoId);
    const firstPageBtn = document.getElementById(firstBtnId);
    const prevPageBtn = document.getElementById(prevBtnId);
    const nextPageBtn = document.getElementById(nextBtnId);
    const lastPageBtn = document.getElementById(lastBtnId);
    const exportBtn = document.getElementById(exportBtnId);
    
    const PAGE_SIZE = ITEMS_PER_PAGE; 
    let currentPage = 1;
    let todosLogs = [];
    let totalPages = 1;

    function drawCurrentPage() {
        const startIndex = (currentPage - 1) * PAGE_SIZE; 
        const endIndex = startIndex + PAGE_SIZE;
        const pageData = todosLogs.slice(startIndex, endIndex); 

        if (pageData.length === 0 && todosLogs.length > 0) {
          tbody.innerHTML = `<tr><td colspan="${COLUMNS}">Nenhum registro nesta página.</td></tr>`;
        } else if (pageData.length === 0) {
          tbody.innerHTML = `<tr><td colspan="${COLUMNS}">Nenhum registro encontrado.</td></tr>`;
        } else {
            tbody.innerHTML = pageData.map(log => `
              <tr>
                <td>${log.idHistorico}</td>
                <td>${log.usuario || 'N/A'}</td>
                <td>${log.acao}</td>
                <td>${log.tipo}</td>
                <td>${formatarDataHora(log.dataHora)}</td>
                <td>${log.encomendaDescricao !== 'Sem referência' ? `${log.encomendaDescricao} (Apt: ${log.encomendaApartamento})` : '-'}</td>
                <td class="actions-column"> 
                  ${log.detalhes ? `<button class="btn-link view-details" data-id="${log.idHistorico}">Ver</button>` : '-'}
                </td>
              </tr>
            `).join('');
            addDetailsButtonListeners();
        }

        totalPages = Math.ceil(todosLogs.length / PAGE_SIZE) || 1;
        if (pageInfoSpan) pageInfoSpan.textContent = `Página ${currentPage} de ${totalPages}`;
        const onFirst = currentPage === 1; const onLast = currentPage === totalPages;
        if (firstPageBtn) firstPageBtn.disabled = onFirst; if (prevPageBtn) prevPageBtn.disabled = onFirst;
        if (nextPageBtn) nextPageBtn.disabled = onLast; if (lastPageBtn) lastPageBtn.disabled = onLast;
    }

     // Busca dados da API
    async function fetchAndSetup() {
        try {
            tbody.innerHTML = `<tr><td colspan="${COLUMNS}">Carregando logs...</td></tr>`;
            const response = await fetchApi('/Sindico/logs'); 
            if (!response.ok) {
              const errJson = await response.json().catch(() => ({}));
              throw new Error(errJson.message || 'Falha ao buscar logs');
            }
            const dados = await response.json();
            todosLogs = dados.message ? [] : (Array.isArray(dados) ? dados : []);
            currentPage = 1; 
            drawCurrentPage(); 
        } catch (err) {
          tbody.innerHTML = `<tr><td colspan="${COLUMNS}">Erro: ${err.message}</td></tr>`;
          if (pageInfoSpan) pageInfoSpan.textContent = `Erro`;
          [firstPageBtn, prevPageBtn, nextPageBtn, lastPageBtn].forEach(btn => { if(btn) btn.disabled = true; });
        }
    }

    [
      { btn: firstPageBtn, action: () => currentPage = 1 },
      { btn: prevPageBtn,  action: () => { if (currentPage > 1) currentPage--; } },
      { btn: nextPageBtn,  action: () => { if (currentPage < totalPages) currentPage++; } },
      { btn: lastPageBtn,  action: () => currentPage = totalPages }
    ].forEach(({ btn, action }) => {
      if (btn && !btn.dataset.listenerAttached) {
        btn.addEventListener('click', () => { action(); drawCurrentPage(); });
        btn.dataset.listenerAttached = 'true';
      }
    });

    if (exportBtn && !exportBtn.dataset.listenerAttached) {
      exportBtn.addEventListener('click', () => {
        if (!todosLogs || todosLogs.length === 0) {
          alert('Não há logs para exportar.');
          return;
        }
        const header = [
          'ID','Usuário','Ação','Tipo','Data/Hora','Encomenda Ref.','Detalhes'
        ];
        const csvRows = [];
        csvRows.push(header.join(';'));
        for (const log of todosLogs) {
          const row = [
            log.idHistorico ?? '',
            log.usuario ?? '',
            log.acao ?? '',
            log.tipo ?? '',
            formatarDataHora(log.dataHora) ?? '',
            log.encomendaDescricao !== 'Sem referência' ? `${log.encomendaDescricao} (Apt: ${log.encomendaApartamento})` : '-',
            log.detalhes ? String(log.detalhes).replace(/[\r\n]+/g, ' ') : '-'
          ].map(v => `"${String(v).replace(/"/g, '""')}"`);
          csvRows.push(row.join(';'));
        }
        const csv = csvRows.join('\n');
        const BOM = '\uFEFF';
        const blob = new Blob([BOM + csv], { type: 'text/csv;charset=utf-8;' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        const ts = new Date().toISOString().slice(0,19).replace(/[:T]/g,'-');
        a.href = url;
        a.download = `relatorio_logs_${ts}.csv`;
        document.body.appendChild(a);
        a.click();
        a.remove();
        URL.revokeObjectURL(url);
      });
      exportBtn.dataset.listenerAttached = 'true';
    }

    function addDetailsButtonListeners() {
      const detailButtons = tbody.querySelectorAll('.view-details');
      detailButtons.forEach(btn => {
        btn.addEventListener('click', (e) => {
          const id = e.currentTarget.dataset.id;
          if (id) {
            window.location.href = `detalhes-log.html?id=${encodeURIComponent(id)}`;
          } else {
            alert('ID do log não encontrado.');
          }
        });
      });
    }

    await fetchAndSetup();
}

/* =========================================
       FUNÇÕES DE AÇÃO (PORTEIRO E MORADOR)
   ========================================= */

// --- Registro por Porteiro ---
async function registrarNovaEncomenda(encomendaData) {
  try {
    const response = await fetchApi('/Porteiro/RegistrarEncomenda', {
      method: 'POST',
      body: JSON.stringify(encomendaData)
    });

    if (!response.ok) {
        const errJson = await response.json().catch(() => ({ message: 'Resposta inválida da API', error: `Status ${response.status}` }));
        const error = new Error(errJson.message || 'Falha ao registrar encomenda');
        error.apiError = errJson.error; 
        throw error; 
    }

    const data = await response.json();
    alert(data.message || 'Encomenda registrada com sucesso!');
    window.location.href = 'entregas.html'; 

  } catch (err) {
    let detailedError = err.apiError || err.message || 'Erro desconhecido'; 
    alert('Erro ao registrar: ' + detailedError);
  }
}

async function confirmarMinhaRetirada(idEncomenda) {
    try {
        const response = await fetchApi(`/Morador/confirmar-recebimento/${idEncomenda}`, {
            method: 'PUT'
        });

        if (!response.ok) {
            const errJson = await response.json().catch(() => ({}));
            throw new Error(errJson.message || 'Falha ao confirmar retirada');
        }

        const data = await response.json();
        alert(data.message || 'Retirada confirmada com sucesso!');
        window.location.href = 'index.html'; 

    } catch (err) {
        alert('Erro: ' + err.message);
    }
}


/* =========================================
       FUNÇÃO DE LOGIN (API) - Mantida
   ========================================= */

(function initLogin() {
  const form = document.getElementById('loginForm');
  if (!form) return;

  const emailEl = document.getElementById('email');
  const senhaEl = document.getElementById('senha');

  async function login(e) {
    e.preventDefault();
    const email = emailEl.value.trim();
    const senha = senhaEl.value;
    if (!email || !senha) return;

    const btn = form.querySelector('button[type="submit"]');
    btn.disabled = true; btn.textContent = 'Entrando...';

    try {
      const resp = await fetch(API_BASE_URL + '/Usuario/login', {
        method: 'POST',
        headers: {'Content-Type': 'application/json'},
        body: JSON.stringify({ email, senha }),
      });
      if (!resp.ok) {
        const err = await resp.json().catch(() => ({}));
        throw new Error(err.message || 'Falha ao autenticar');
      }
      const data = await resp.json();
      localStorage.setItem(TOKEN_KEY, data.token);
      localStorage.setItem(USER_KEY, JSON.stringify(data.usuario));

      if (data.usuario.tipoUsuario === 'Morador') window.location.href = 'morador/index.html';
      else if (data.usuario.tipoUsuario === 'Porteiro') window.location.href = 'porteiro/index.html';
      else if (data.usuario.tipoUsuario === 'Sindico') window.location.href = 'admin/index.html';
      else {
          alert('Tipo de usuário não reconhecido: ' + data.usuario.tipoUsuario);
          btn.disabled = false; btn.textContent = 'Entrar';
      }

    } catch (err) {
      alert(err.message.includes('Failed to fetch') ? 'Erro ao conectar com a API. O back-end está rodando?' : (err.message || 'Erro de login'));
      btn.disabled = false; btn.textContent = 'Entrar'; 
    } 
    
  }
  form.addEventListener('submit', login);
})();
