<script setup>
import { ref, computed, onMounted } from 'vue'
import api from './services/api'

const active = ref('dashboard')
const loading = ref(false)
const error = ref('')
const success = ref('')
const books = ref([])
const patrons = ref([])
const tags = ref([])
const transactions = ref([])
const summary = ref({})
const search = ref('')
const bookForm = ref({ bookId: '', callNo: '', title: '', author: '', marcData: '' })
const patronForm = ref({ memberId: '', name: '', phone: '', email: '', isActive: true, expiryDate: '' })
const tagForm = ref({ rfidNo: '', bookId: '', registeredBy: '' })
const circulation = ref({ memberId: '', rfidNo: '', loanDays: 14 })
const editingBook = ref(false)
const editingPatron = ref(false)

const filteredBooks = computed(() => {
  const s = search.value.toLowerCase()
  if (!s) return books.value
  return books.value.filter(b => [b.title,b.author,b.callNo,b.bookId].some(v => (v || '').toLowerCase().includes(s)) ||
    (b.rfidTags || []).some(t => t.rfidNo.toLowerCase().includes(s)))
})

function notifyError(e) {
  error.value = e.response?.data?.message || e.message || 'Something went wrong.'
  success.value = ''
}
function notifySuccess(message) {
  success.value = message
  error.value = ''
  setTimeout(() => { success.value = '' }, 3000)
}
async function loadAll() {
  loading.value = true
  error.value = ''
  try {
    const [b,p,t,tr,s] = await Promise.all([
      api.get('/books'), api.get('/patrons'), api.get('/rfidtags'),
      api.get('/circulation/transactions'), api.get('/reports/summary')
    ])
    books.value = b.data; patrons.value = p.data; tags.value = t.data
    transactions.value = tr.data; summary.value = s.data
  } catch (e) { notifyError(e) }
  finally { loading.value = false }
}
function resetBook() {
  bookForm.value = { bookId:'', callNo:'', title:'', author:'', marcData:'' }
  editingBook.value = false
}
async function saveBook() {
  try {
    if (editingBook.value) await api.put(`/books/${bookForm.value.bookId}`, bookForm.value)
    else await api.post('/books', bookForm.value)
    resetBook(); await loadAll(); notifySuccess('Book saved successfully.')
  } catch (e) { notifyError(e) }
}
function editBook(book) {
  bookForm.value = { ...book }; editingBook.value = true; active.value = 'books'
}
async function deleteBook(id) {
  if (!confirm('Delete this book?')) return
  try { await api.delete(`/books/${id}`); await loadAll(); notifySuccess('Book deleted.') }
  catch (e) { notifyError(e) }
}
function resetPatron() {
  patronForm.value = { memberId:'', name:'', phone:'', email:'', isActive:true, expiryDate:'' }
  editingPatron.value = false
}
async function savePatron() {
  try {
    const data = { ...patronForm.value, expiryDate: patronForm.value.expiryDate || null }
    if (editingPatron.value) await api.put(`/patrons/${data.memberId}`, data)
    else await api.post('/patrons', data)
    resetPatron(); await loadAll(); notifySuccess('Member saved successfully.')
  } catch (e) { notifyError(e) }
}
function editPatron(p) {
  patronForm.value = { ...p, expiryDate: p.expiryDate?.slice(0,10) || '' }
  editingPatron.value = true; active.value = 'patrons'
}
async function deletePatron(id) {
  if (!confirm('Delete this member?')) return
  try { await api.delete(`/patrons/${id}`); await loadAll(); notifySuccess('Member deleted.') }
  catch (e) { notifyError(e) }
}
async function registerTag() {
  try { await api.post('/rfidtags', tagForm.value); tagForm.value = { rfidNo:'', bookId:'', registeredBy:'' }; await loadAll(); notifySuccess('RFID tag registered.') }
  catch (e) { notifyError(e) }
}
async function borrowBook() {
  try { await api.post('/circulation/borrow', circulation.value); circulation.value.rfidNo = ''; await loadAll(); notifySuccess('Book borrowed successfully.') }
  catch (e) { notifyError(e) }
}
async function returnBook() {
  try { await api.post('/circulation/return', { rfidNo: circulation.value.rfidNo }); circulation.value.rfidNo = ''; await loadAll(); notifySuccess('Book returned successfully.') }
  catch (e) { notifyError(e) }
}
async function scanTag() {
  try {
    const { data } = await api.get(`/rfidtags/${encodeURIComponent(circulation.value.rfidNo)}/scan`)
    notifySuccess(`Scanned: ${data.book?.title || data.bookId} (${data.tagStatus})`)
  } catch (e) { notifyError(e) }
}
async function exportMarc() {
  try {
    const response = await api.get('/marc/export', { responseType: 'blob' })
    const url = URL.createObjectURL(response.data)
    const a = document.createElement('a'); a.href = url; a.download = 'library-marc-export.txt'; a.click()
    URL.revokeObjectURL(url); notifySuccess('MARC export downloaded.')
  } catch (e) { notifyError(e) }
}
async function importMarc(event) {
  const file = event.target.files[0]
  if (!file) return
  const form = new FormData(); form.append('file', file)
  try { const { data } = await api.post('/marc/import', form, { headers: {'Content-Type':'multipart/form-data'} }); await loadAll(); notifySuccess(`${data.imported} MARC records imported.`) }
  catch (e) { notifyError(e) }
  event.target.value = ''
}
onMounted(loadAll)
</script>

<template>
  <div class="layout">
    <aside class="sidebar">
      <div class="brand"><div class="brand-mark">R</div><div><strong>RFID Library</strong><small>Management System</small></div></div>
      <div class="nav-label">WORKSPACE</div>
      <button v-for="item in [
        ['dashboard','▦','Dashboard'],['books','▤','Manage Books'],['patrons','♙','Patrons'],
        ['rfid','⌁','RFID Registration'],['circulation','⇄','Borrow / Return'],
        ['transactions','▧','Transactions'],['reports','▥','Reports']
      ]" :key="item[0]" class="nav-item" :class="{selected:active===item[0]}" @click="active=item[0]">
        <span>{{item[1]}}</span>{{item[2]}}
      </button>
      <div class="sidebar-bottom"><span class="online-dot"></span> API workspace</div>
    </aside>

    <main class="main">
      <header class="topbar">
        <div><div class="eyebrow">LIBRARY OPERATIONS</div><h1>{{({dashboard:'Dashboard',books:'Manage Books',patrons:'Patron Management',rfid:'RFID Registration',circulation:'Book Circulation',transactions:'Transactions',reports:'Reports'})[active]}}</h1></div>
        <button class="secondary" @click="loadAll">↻ Refresh data</button>
      </header>

      <div v-if="error" class="alert error">{{error}}</div>
      <div v-if="success" class="alert success">{{success}}</div>
      <div v-if="loading" class="loading">Loading library data…</div>

      <section v-if="active==='dashboard'">
        <div class="welcome"><div><div class="eyebrow light">LIBRARY OVERVIEW</div><h2>Good day, Librarian</h2><p>Manage your collection, members and circulation from one place.</p></div><div class="welcome-icon">▤</div></div>
        <div class="stats">
          <div class="stat"><span>Total books</span><strong>{{summary.totalBooks ?? 0}}</strong><small>Registered collection</small></div>
          <div class="stat"><span>Available books</span><strong>{{summary.availableBooks ?? 0}}</strong><small>Ready to borrow</small></div>
          <div class="stat"><span>Borrowed books</span><strong>{{summary.borrowedBooks ?? 0}}</strong><small>Currently on loan</small></div>
          <div class="stat"><span>Active members</span><strong>{{summary.activePatrons ?? 0}}</strong><small>Valid memberships</small></div>
        </div>
        <div class="panel"><div class="panel-heading"><div><h3>Recent transactions</h3><p>Latest borrowing and return activity</p></div><button class="text-button" @click="active='transactions'">View all →</button></div>
          <div class="table-wrap"><table><thead><tr><th>Transaction</th><th>Member</th><th>Book</th><th>Date</th><th>Status</th></tr></thead>
          <tbody><tr v-for="t in transactions.slice(0,6)" :key="t.transId"><td>{{t.transId}}</td><td>{{t.memberName}}</td><td>{{t.bookTitle}}</td><td>{{new Date(t.borrowDate).toLocaleDateString()}}</td><td><span class="status" :class="t.status.toLowerCase()">{{t.status}}</span></td></tr><tr v-if="!transactions.length"><td colspan="5" class="empty">No transactions yet.</td></tr></tbody></table></div>
        </div>
      </section>

      <section v-if="active==='books'" class="two-column">
        <div class="panel form-panel"><div class="panel-heading"><div><h3>{{editingBook?'Edit book':'Add a book'}}</h3><p>Enter cataloguing information</p></div></div>
          <form @submit.prevent="saveBook">
            <label>Call Number *</label><input v-model="bookForm.callNo" required placeholder="QA76.76.C65"/>
            <label>Book title *</label><input v-model="bookForm.title" required placeholder="Clean Code"/>
            <label>Author</label><input v-model="bookForm.author" placeholder="Robert C. Martin"/>
            <label>MARC data</label><textarea v-model="bookForm.marcData" rows="4" placeholder="MARC record text…"></textarea>
            <div class="form-actions"><button class="primary">{{editingBook?'Update book':'Save book'}}</button><button type="button" class="secondary" @click="resetBook">Clear</button></div>
          </form>
        </div>
        <div class="panel"><div class="panel-heading"><div><h3>Book catalogue</h3><p>{{books.length}} records</p></div></div>
          <input class="search" v-model="search" placeholder="Search title, author or call number…"/>
          <div class="table-wrap"><table><thead><tr><th>Book</th><th>Call No.</th><th>Status</th><th>Actions</th></tr></thead>
          <tbody><tr v-for="b in filteredBooks" :key="b.bookId"><td><strong>{{b.title}}</strong><small class="subline">{{b.author}}</small></td><td>{{b.callNo}}</td><td><span class="status" :class="b.isAvailable?'available':'borrowed'">{{b.isAvailable?'Available':'Borrowed'}}</span></td><td class="actions"><button class="mini" @click="editBook(b)">Edit</button><button class="mini danger" @click="deleteBook(b.bookId)">Delete</button></td></tr><tr v-if="!filteredBooks.length"><td colspan="4" class="empty">No books found.</td></tr></tbody></table></div>
        </div>
      </section>

      <section v-if="active==='patrons'" class="two-column">
        <div class="panel form-panel"><div class="panel-heading"><div><h3>{{editingPatron?'Edit member':'Register member'}}</h3><p>Patron details and membership</p></div></div>
          <form @submit.prevent="savePatron">
            <label>Full name *</label><input v-model="patronForm.name" required placeholder="Member name"/>
            <label>Phone</label><input v-model="patronForm.phone" placeholder="09xxxxxxxxx"/>
            <label>Email</label><input v-model="patronForm.email" type="email" placeholder="member@email.com"/>
            <label>Expiry date</label><input v-model="patronForm.expiryDate" type="date"/>
            <label class="check-label"><input v-model="patronForm.isActive" type="checkbox"/> Membership active</label>
            <div class="form-actions"><button class="primary">{{editingPatron?'Update member':'Register member'}}</button><button type="button" class="secondary" @click="resetPatron">Clear</button></div>
          </form>
        </div>
        <div class="panel"><div class="panel-heading"><div><h3>Patrons</h3><p>{{patrons.length}} registered members</p></div></div>
          <div class="table-wrap"><table><thead><tr><th>Member</th><th>Phone</th><th>Status</th><th>Actions</th></tr></thead><tbody>
          <tr v-for="p in patrons" :key="p.memberId"><td><strong>{{p.name}}</strong><small class="subline">{{p.memberId}}</small></td><td>{{p.phone}}</td><td><span class="status" :class="p.isActive?'available':'inactive'">{{p.isActive?'Active':'Inactive'}}</span></td><td class="actions"><button class="mini" @click="editPatron(p)">Edit</button><button class="mini danger" @click="deletePatron(p.memberId)">Delete</button></td></tr>
          <tr v-if="!patrons.length"><td colspan="4" class="empty">No members yet.</td></tr></tbody></table></div>
        </div>
      </section>

      <section v-if="active==='rfid'" class="two-column">
        <div class="panel form-panel"><div class="panel-heading"><div><h3>Register RFID tag</h3><p>Pair one RFID number with a book</p></div></div>
          <form @submit.prevent="registerTag">
            <label>RFID number *</label><input v-model="tagForm.rfidNo" required placeholder="RFID-99012"/>
            <label>Book *</label><select v-model="tagForm.bookId" required><option value="" disabled>Select a book</option><option v-for="b in books" :value="b.bookId">{{b.title}} — {{b.callNo}}</option></select>
            <label>Registered by (Staff ID)</label><input v-model="tagForm.registeredBy" placeholder="STAFF-001"/>
            <div class="form-actions"><button class="primary">Save & register tag</button></div>
          </form>
        </div>
        <div class="panel"><div class="panel-heading"><div><h3>Registered tags</h3><p>{{tags.length}} RFID tags</p></div></div>
          <div class="table-wrap"><table><thead><tr><th>RFID No.</th><th>Book</th><th>Status</th></tr></thead><tbody>
          <tr v-for="t in tags" :key="t.rfidNo"><td><strong>{{t.rfidNo}}</strong></td><td>{{t.book?.title || t.bookId}}</td><td><span class="status" :class="t.tagStatus.toLowerCase()">{{t.tagStatus}}</span></td></tr>
          <tr v-if="!tags.length"><td colspan="3" class="empty">No RFID tags registered.</td></tr></tbody></table></div>
        </div>
      </section>

      <section v-if="active==='circulation'" class="two-column circulation-grid">
        <div class="panel form-panel"><div class="panel-heading"><div><h3>Borrow a book</h3><p>Validate membership and scan the RFID tag</p></div></div>
          <form @submit.prevent="borrowBook">
            <label>Member ID *</label><input v-model="circulation.memberId" required placeholder="MEM-2026-001"/>
            <label>RFID number *</label><input v-model="circulation.rfidNo" required placeholder="RFID-99012"/>
            <label>Loan period (days)</label><input v-model.number="circulation.loanDays" type="number" min="1" max="60"/>
            <div class="form-actions"><button type="button" class="secondary" @click="scanTag">Scan / Check tag</button><button class="primary">Borrow book</button></div>
          </form>
        </div>
        <div class="panel form-panel"><div class="panel-heading"><div><h3>Return a book</h3><p>Scan the book RFID tag to complete return</p></div></div>
          <form @submit.prevent="returnBook">
            <label>RFID number *</label><input v-model="circulation.rfidNo" required placeholder="RFID-99012"/>
            <div class="form-actions"><button class="primary">Return book</button></div>
          </form>
          <div class="note">The API checks that the book has an active borrowing transaction before returning it.</div>
        </div>
      </section>

      <section v-if="active==='transactions' || active==='reports'" class="panel">
        <div class="panel-heading"><div><h3>{{active==='reports'?'Library reports':'Transaction history'}}</h3><p>{{transactions.length}} transaction records</p></div></div>
        <div v-if="active==='reports'" class="report-actions"><button class="secondary" @click="exportMarc">Export MARC text</button><label class="upload secondary">Import MARC text<input type="file" accept=".txt,.mrc,.marc" @change="importMarc" hidden/></label><button class="secondary" @click="api.get('/reports/overdue').then(r=>notifySuccess(`${r.data.length} overdue books found.`)).catch(notifyError)">Check overdue</button></div>
        <div class="table-wrap"><table><thead><tr><th>Transaction ID</th><th>Member</th><th>Book</th><th>Borrow date</th><th>Due date</th><th>Return date</th><th>Status</th></tr></thead><tbody>
          <tr v-for="t in transactions" :key="t.transId"><td>{{t.transId}}</td><td>{{t.memberName}}</td><td>{{t.bookTitle}}</td><td>{{new Date(t.borrowDate).toLocaleDateString()}}</td><td>{{new Date(t.dueDate).toLocaleDateString()}}</td><td>{{t.returnDate?new Date(t.returnDate).toLocaleDateString():'—'}}</td><td><span class="status" :class="t.status.toLowerCase()">{{t.status}}</span></td></tr>
          <tr v-if="!transactions.length"><td colspan="7" class="empty">No transaction records yet.</td></tr></tbody></table></div>
      </section>
      <footer>RFID Library Management System <span>·</span> Vue 3 + ASP.NET Core + MySQL</footer>
    </main>
  </div>
</template>
