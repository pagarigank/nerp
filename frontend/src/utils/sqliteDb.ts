/**
 * Client-Side SQLite Database Engine (sql.js WebAssembly)
 * Persists SQLite binary (.sqlite3) to IndexedDB for seamless offline selling.
 */
import initSqlJs, { type Database as SqliteDatabase } from 'sql.js'

export interface RouteRecord {
  id: string
  salesRepId: string
  customerId: string
  customerCode: string
  customerName: string
  contactName: string
  phone: string
  address: string
  dayOfWeek: number
  sequenceOrder: number
  estimatedTime?: string | undefined
  notes?: string | undefined
  creditLimit: number
  currentBalance: number
}

export interface ProductRecord {
  id: string
  itemCode: string
  description: string
  basePrice: number
  unitOfMeasure: string
  itemCategoryId?: string | undefined
  categoryName?: string | undefined
  onHandQty: number
}

export interface PricingRuleRecord {
  id: string
  customerId?: string | undefined
  itemId?: string | undefined
  itemCategoryId?: string | undefined
  discountPercent: number
  unitPriceOverride?: number | undefined
  minQty?: number | undefined
  prioritySequence: number
}

export interface SalesCallRecord {
  id: string
  companyId: string
  salesRepId: string
  customerId: string
  customerName: string
  routeId?: string | undefined
  callDate: string
  startTime: string
  endTime?: string | undefined
  status: string
  notes?: string | undefined
  grandTotal: number
  isSynced: number // 0 or 1
  syncedAt?: string | undefined
  serverOrderId?: string | undefined
  serverOrderNumber?: string | undefined
  syncError?: string | undefined
}

export interface SalesCallItemRecord {
  id: string
  salesCallId: string
  itemId: string
  itemCode: string
  description: string
  sellingPrice: number
  orderQty: number
  unitOfMeasure: string
  discountPercent: number
  lineTotal: number
}

export interface DatabaseStats {
  routesCount: number
  productsCount: number
  pricingRulesCount: number
  salesCallsCount: number
  unsyncedCallsCount: number
  lastSyncTime?: string | undefined
  databaseSizeKb: number
}

const DB_STORE_NAME = 'nerp_sqlite_store'
const DB_KEY = 'salesperson_sqlite_db'

type SqlJsModule = Awaited<ReturnType<typeof initSqlJs>>

class SqliteService {
  private db: SqliteDatabase | null = null
  private initPromise: Promise<SqliteDatabase> | null = null
  private sqlModule: SqlJsModule | null = null

  private async getSql(): Promise<SqlJsModule> {
    if (this.sqlModule) return this.sqlModule
    this.sqlModule = await initSqlJs({
      locateFile: () => '/sql-wasm.wasm',
    })
    return this.sqlModule
  }

  public async getDb(): Promise<SqliteDatabase> {
    if (this.db) return this.db
    if (this.initPromise) return this.initPromise

    this.initPromise = (async () => {
      const SQL = await this.getSql()

      const savedData = await this.loadFromIndexedDb()
      if (savedData && savedData.byteLength > 0) {
        try {
          this.db = new SQL.Database(savedData)
        } catch {
          this.db = new SQL.Database()
        }
      } else {
        this.db = new SQL.Database()
      }

      this.createSchema(this.db)
      await this.saveToIndexedDb()
      return this.db
    })()

    return this.initPromise
  }

  private createSchema(db: SqliteDatabase) {
    db.run(`
      CREATE TABLE IF NOT EXISTS meta (
        key TEXT PRIMARY KEY,
        value TEXT
      );

      CREATE TABLE IF NOT EXISTS routes (
        id TEXT PRIMARY KEY,
        sales_rep_id TEXT,
        customer_id TEXT,
        customer_code TEXT,
        customer_name TEXT,
        contact_name TEXT,
        phone TEXT,
        address TEXT,
        day_of_week INTEGER,
        sequence_order INTEGER,
        estimated_time TEXT,
        notes TEXT,
        credit_limit REAL,
        current_balance REAL
      );

      CREATE TABLE IF NOT EXISTS products (
        id TEXT PRIMARY KEY,
        item_code TEXT,
        description TEXT,
        base_price REAL,
        unit_of_measure TEXT,
        item_category_id TEXT,
        category_name TEXT,
        on_hand_qty REAL
      );

      CREATE TABLE IF NOT EXISTS pricing_rules (
        id TEXT PRIMARY KEY,
        customer_id TEXT,
        item_id TEXT,
        item_category_id TEXT,
        discount_percent REAL,
        unit_price_override REAL,
        min_qty REAL,
        priority_sequence INTEGER
      );

      CREATE TABLE IF NOT EXISTS sales_calls (
        id TEXT PRIMARY KEY,
        company_id TEXT,
        sales_rep_id TEXT,
        customer_id TEXT,
        customer_name TEXT,
        route_id TEXT,
        call_date TEXT,
        start_time TEXT,
        end_time TEXT,
        status TEXT,
        notes TEXT,
        grand_total REAL,
        is_synced INTEGER DEFAULT 0,
        synced_at TEXT,
        server_order_id TEXT,
        server_order_number TEXT,
        sync_error TEXT
      );

      CREATE TABLE IF NOT EXISTS sales_call_items (
        id TEXT PRIMARY KEY,
        sales_call_id TEXT,
        item_id TEXT,
        item_code TEXT,
        description TEXT,
        selling_price REAL,
        order_qty REAL,
        unit_of_measure TEXT,
        discount_percent REAL,
        line_total REAL
      );
    `)
    this.migrateSchema(db)
  }

  private migrateSchema(db: SqliteDatabase) {
    const hasColumn = (table: string, column: string): boolean => {
      const res = db.exec(`PRAGMA table_info(${table});`)
      if (!res.length) return false
      const cols = (res[0]?.values ?? []).map((v) => String(v[1]))
      return cols.includes(column)
    }

    if (!hasColumn('sales_calls', 'route_id')) {
      db.run('ALTER TABLE sales_calls ADD COLUMN route_id TEXT;')
    }
    if (!hasColumn('sales_call_items', 'unit_of_measure')) {
      db.run('ALTER TABLE sales_call_items ADD COLUMN unit_of_measure TEXT;')
    }
  }

  // --- IndexedDB Persistence ---
  private async openIdb(): Promise<IDBDatabase> {
    return new Promise((resolve, reject) => {
      const req = indexedDB.open('nerp_offline_db', 1)
      req.onupgradeneeded = () => {
        const db = req.result
        if (!db.objectStoreNames.contains(DB_STORE_NAME)) {
          db.createObjectStore(DB_STORE_NAME)
        }
      }
      req.onsuccess = () => resolve(req.result)
      req.onerror = () => reject(req.error)
    })
  }

  public async saveToIndexedDb(): Promise<void> {
    if (!this.db) return
    const data = this.db.export()
    const idb = await this.openIdb()
    return new Promise((resolve, reject) => {
      const tx = idb.transaction(DB_STORE_NAME, 'readwrite')
      const store = tx.objectStore(DB_STORE_NAME)
      const putReq = store.put(data, DB_KEY)
      putReq.onsuccess = () => resolve()
      putReq.onerror = () => reject(putReq.error)
    })
  }

  private async loadFromIndexedDb(): Promise<Uint8Array | null> {
    try {
      const idb = await this.openIdb()
      return new Promise((resolve) => {
        const tx = idb.transaction(DB_STORE_NAME, 'readonly')
        const store = tx.objectStore(DB_STORE_NAME)
        const getReq = store.get(DB_KEY)
        getReq.onsuccess = () => {
          if (getReq.result instanceof Uint8Array) {
            resolve(getReq.result)
          } else {
            resolve(null)
          }
        }
        getReq.onerror = () => resolve(null)
      })
    } catch {
      return null
    }
  }

  // --- Sync Package Import into SQLite ---
  public async importSyncPackage(packageData: {
    serverTime: string
    routes: RouteRecord[]
    products: ProductRecord[]
    pricingRules: PricingRuleRecord[]
  }): Promise<void> {
    const db = await this.getDb()

    db.run('BEGIN TRANSACTION;')
    try {
      // Update meta
      db.run('INSERT OR REPLACE INTO meta (key, value) VALUES (?, ?);', ['last_sync_time', packageData.serverTime])

      // Clear cached sync tables (preserve sales_calls!)
      db.run('DELETE FROM routes;')
      db.run('DELETE FROM products;')
      db.run('DELETE FROM pricing_rules;')

      // Insert Routes
      const routeStmt = db.prepare(`
        INSERT INTO routes (
          id, sales_rep_id, customer_id, customer_code, customer_name,
          contact_name, phone, address, day_of_week, sequence_order,
          estimated_time, notes, credit_limit, current_balance
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
      `)
      for (const r of packageData.routes) {
        routeStmt.run([
          r.id,
          r.salesRepId,
          r.customerId,
          r.customerCode,
          r.customerName,
          r.contactName || '',
          r.phone || '',
          r.address || '',
          r.dayOfWeek,
          r.sequenceOrder,
          r.estimatedTime || '',
          r.notes || '',
          r.creditLimit || 0,
          r.currentBalance || 0,
        ])
      }
      routeStmt.free()

      // Insert Products
      const prodStmt = db.prepare(`
        INSERT INTO products (
          id, item_code, description, base_price, unit_of_measure,
          item_category_id, category_name, on_hand_qty
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?);
      `)
      for (const p of packageData.products) {
        prodStmt.run([
          p.id,
          p.itemCode,
          p.description,
          p.basePrice,
          p.unitOfMeasure,
          p.itemCategoryId || '',
          p.categoryName || '',
          p.onHandQty || 0,
        ])
      }
      prodStmt.free()

      // Insert Pricing Rules
      const ruleStmt = db.prepare(`
        INSERT INTO pricing_rules (
          id, customer_id, item_id, item_category_id,
          discount_percent, unit_price_override, min_qty, priority_sequence
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?);
      `)
      for (const pr of packageData.pricingRules) {
        ruleStmt.run([
          pr.id,
          pr.customerId || '',
          pr.itemId || '',
          pr.itemCategoryId || '',
          pr.discountPercent || 0,
          pr.unitPriceOverride ?? null,
          pr.minQty ?? null,
          pr.prioritySequence || 100,
        ])
      }
      ruleStmt.free()

      db.run('COMMIT;')
      await this.saveToIndexedDb()
    } catch (err) {
      db.run('ROLLBACK;')
      throw err
    }
  }

  // --- Query Methods ---
  public async getRoutesByDay(dayOfWeek: number): Promise<RouteRecord[]> {
    const db = await this.getDb()
    const stmt = db.prepare(
      'SELECT id, sales_rep_id, customer_id, customer_code, customer_name, contact_name, phone, address, day_of_week, sequence_order, estimated_time, notes, credit_limit, current_balance FROM routes WHERE day_of_week = ? ORDER BY sequence_order ASC;',
    )
    stmt.bind([dayOfWeek])
    const list: RouteRecord[] = []
    while (stmt.step()) {
      const row = stmt.getAsObject() as Record<string, unknown>
      list.push({
        id: String(row.id),
        salesRepId: String(row.sales_rep_id),
        customerId: String(row.customer_id),
        customerCode: String(row.customer_code),
        customerName: String(row.customer_name),
        contactName: String(row.contact_name || ''),
        phone: String(row.phone || ''),
        address: String(row.address || ''),
        dayOfWeek: Number(row.day_of_week),
        sequenceOrder: Number(row.sequence_order),
        estimatedTime: row.estimated_time ? String(row.estimated_time) : undefined,
        notes: row.notes ? String(row.notes) : undefined,
        creditLimit: Number(row.credit_limit || 0),
        currentBalance: Number(row.current_balance || 0),
      })
    }
    stmt.free()
    return list
  }

  public async getAllRoutes(): Promise<RouteRecord[]> {
    const db = await this.getDb()
    const stmt = db.prepare(
      'SELECT id, sales_rep_id, customer_id, customer_code, customer_name, contact_name, phone, address, day_of_week, sequence_order, estimated_time, notes, credit_limit, current_balance FROM routes ORDER BY day_of_week ASC, sequence_order ASC;',
    )
    const list: RouteRecord[] = []
    while (stmt.step()) {
      const row = stmt.getAsObject() as Record<string, unknown>
      list.push({
        id: String(row.id),
        salesRepId: String(row.sales_rep_id),
        customerId: String(row.customer_id),
        customerCode: String(row.customer_code),
        customerName: String(row.customer_name),
        contactName: String(row.contact_name || ''),
        phone: String(row.phone || ''),
        address: String(row.address || ''),
        dayOfWeek: Number(row.day_of_week),
        sequenceOrder: Number(row.sequence_order),
        estimatedTime: row.estimated_time ? String(row.estimated_time) : undefined,
        notes: row.notes ? String(row.notes) : undefined,
        creditLimit: Number(row.credit_limit || 0),
        currentBalance: Number(row.current_balance || 0),
      })
    }
    stmt.free()
    return list
  }

  public async getCustomerById(customerId: string): Promise<RouteRecord | null> {
    const db = await this.getDb()
    const stmt = db.prepare('SELECT * FROM routes WHERE customer_id = ? LIMIT 1;')
    stmt.bind([customerId])
    let res: RouteRecord | null = null
    if (stmt.step()) {
      const row = stmt.getAsObject() as Record<string, unknown>
      res = {
        id: String(row.id),
        salesRepId: String(row.sales_rep_id),
        customerId: String(row.customer_id),
        customerCode: String(row.customer_code),
        customerName: String(row.customer_name),
        contactName: String(row.contact_name || ''),
        phone: String(row.phone || ''),
        address: String(row.address || ''),
        dayOfWeek: Number(row.day_of_week),
        sequenceOrder: Number(row.sequence_order),
        estimatedTime: row.estimated_time ? String(row.estimated_time) : undefined,
        notes: row.notes ? String(row.notes) : undefined,
        creditLimit: Number(row.credit_limit || 0),
        currentBalance: Number(row.current_balance || 0),
      }
    }
    stmt.free()
    return res
  }

  public async getAllProducts(): Promise<ProductRecord[]> {
    const db = await this.getDb()
    const stmt = db.prepare('SELECT id, item_code, description, base_price, unit_of_measure, item_category_id, category_name, on_hand_qty FROM products ORDER BY description ASC;')
    const list: ProductRecord[] = []
    while (stmt.step()) {
      const row = stmt.getAsObject() as Record<string, unknown>
      list.push({
        id: String(row.id),
        itemCode: String(row.item_code),
        description: String(row.description),
        basePrice: Number(row.base_price || 0),
        unitOfMeasure: String(row.unit_of_measure || 'EA'),
        itemCategoryId: row.item_category_id ? String(row.item_category_id) : undefined,
        categoryName: row.category_name ? String(row.category_name) : undefined,
        onHandQty: Number(row.on_hand_qty || 0),
      })
    }
    stmt.free()
    return list
  }

  /**
   * Evaluates pricing rules in SQLite for customer, item, and category.
   * Mirrors server PricingEngine: filter by matchable dimensions, then order by
   * PrioritySequence with customer-specific preference.
   */
  public async calculateItemPrice(
    customerId: string,
    itemId: string,
    itemCategoryId: string | undefined,
    qty: number,
    basePrice: number,
  ): Promise<{ sellingPrice: number; discountPercent: number; ruleApplied?: string | undefined }> {
    const db = await this.getDb()
    const stmt = db.prepare(`
      SELECT id, discount_percent, unit_price_override
      FROM pricing_rules
      WHERE (customer_id = '' OR customer_id IS NULL OR customer_id = ?)
        AND (item_id = '' OR item_id IS NULL OR item_id = ?)
        AND (item_category_id = '' OR item_category_id IS NULL OR item_category_id = ?)
        AND (min_qty IS NULL OR min_qty <= ?)
      ORDER BY priority_sequence ASC,
               (CASE WHEN customer_id != '' AND customer_id = ? THEN 0 ELSE 1 END) ASC
      LIMIT 1;
    `)
    stmt.bind([customerId, itemId, itemCategoryId || '', qty, customerId])

    let sellingPrice = basePrice
    let discountPercent = 0
    let ruleApplied: string | undefined

    if (stmt.step()) {
      const row = stmt.getAsObject() as Record<string, unknown>
      ruleApplied = String(row.id)
      if (row.unit_price_override != null && Number(row.unit_price_override) > 0) {
        sellingPrice = Number(row.unit_price_override)
      }
      if (row.discount_percent != null && Number(row.discount_percent) > 0) {
        discountPercent = Number(row.discount_percent)
        sellingPrice = Math.round(sellingPrice * (1 - discountPercent / 100) * 100) / 100
      }
    }
    stmt.free()

    return { sellingPrice, discountPercent, ruleApplied }
  }

  // --- Sales Call Recording (Offline First) ---
  public async saveSalesCall(call: {
    id: string
    companyId: string
    salesRepId: string
    customerId: string
    customerName: string
    routeId?: string | undefined
    callDate: string
    startTime: string
    endTime?: string | undefined
    status: string
    notes?: string | undefined
    grandTotal: number
    lines: {
      itemId: string
      itemCode: string
      description: string
      sellingPrice: number
      orderQty: number
      unitOfMeasure?: string | undefined
      discountPercent: number
      lineTotal: number
    }[]
  }): Promise<void> {
    const db = await this.getDb()
    db.run('BEGIN TRANSACTION;')
    try {
      // Insert Call
      const callStmt = db.prepare(`
        INSERT OR REPLACE INTO sales_calls (
          id, company_id, sales_rep_id, customer_id, customer_name, route_id,
          call_date, start_time, end_time, status, notes, grand_total, is_synced
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 0);
      `)
      callStmt.run([
        call.id,
        call.companyId,
        call.salesRepId,
        call.customerId,
        call.customerName,
        call.routeId || '',
        call.callDate,
        call.startTime,
        call.endTime || '',
        call.status,
        call.notes || '',
        call.grandTotal,
      ])
      callStmt.free()

      // Delete existing lines if updating
      db.run('DELETE FROM sales_call_items WHERE sales_call_id = ?;', [call.id])

      // Insert Lines
      const lineStmt = db.prepare(`
        INSERT INTO sales_call_items (
          id, sales_call_id, item_id, item_code, description,
          selling_price, order_qty, unit_of_measure, discount_percent, line_total
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
      `)

      for (const line of call.lines) {
        const lineId = crypto.randomUUID()
        lineStmt.run([
          lineId,
          call.id,
          line.itemId,
          line.itemCode,
          line.description,
          line.sellingPrice,
          line.orderQty,
          line.unitOfMeasure || 'EA',
          line.discountPercent,
          line.lineTotal,
        ])
      }
      lineStmt.free()

      db.run('COMMIT;')
      await this.saveToIndexedDb()
    } catch (err) {
      db.run('ROLLBACK;')
      throw err
    }
  }

  public async getUnsyncedSalesCalls(): Promise<(SalesCallRecord & { lines: SalesCallItemRecord[] })[]> {
    const db = await this.getDb()
    const callStmt = db.prepare('SELECT * FROM sales_calls WHERE is_synced = 0 ORDER BY start_time ASC;')
    const calls: (SalesCallRecord & { lines: SalesCallItemRecord[] })[] = []

    while (callStmt.step()) {
      const row = callStmt.getAsObject() as Record<string, unknown>
      const callId = String(row.id)

      // Query items for this call
      const itemStmt = db.prepare('SELECT * FROM sales_call_items WHERE sales_call_id = ?;')
      itemStmt.bind([callId])
      const lines: SalesCallItemRecord[] = []
      while (itemStmt.step()) {
        const itemRow = itemStmt.getAsObject() as Record<string, unknown>
        lines.push({
          id: String(itemRow.id),
          salesCallId: String(itemRow.sales_call_id),
          itemId: String(itemRow.item_id),
          itemCode: String(itemRow.item_code),
          description: String(itemRow.description),
          sellingPrice: Number(itemRow.selling_price || 0),
          orderQty: Number(itemRow.order_qty || 0),
          unitOfMeasure: String(itemRow.unit_of_measure || 'EA'),
          discountPercent: Number(itemRow.discount_percent || 0),
          lineTotal: Number(itemRow.line_total || 0),
        })
      }
      itemStmt.free()

      calls.push({
        id: callId,
        companyId: String(row.company_id),
        salesRepId: String(row.sales_rep_id),
        customerId: String(row.customer_id),
        customerName: String(row.customer_name),
        routeId: row.route_id ? String(row.route_id) : undefined,
        callDate: String(row.call_date),
        startTime: String(row.start_time),
        endTime: row.end_time ? String(row.end_time) : undefined,
        status: String(row.status),
        notes: row.notes ? String(row.notes) : undefined,
        grandTotal: Number(row.grand_total || 0),
        isSynced: Number(row.is_synced || 0),
        syncedAt: row.synced_at ? String(row.synced_at) : undefined,
        serverOrderId: row.server_order_id ? String(row.server_order_id) : undefined,
        serverOrderNumber: row.server_order_number ? String(row.server_order_number) : undefined,
        lines,
      })
    }
    callStmt.free()
    return calls
  }

  public async getAllSalesCalls(): Promise<(SalesCallRecord & { lines: SalesCallItemRecord[] })[]> {
    const db = await this.getDb()
    const callStmt = db.prepare('SELECT * FROM sales_calls ORDER BY start_time DESC LIMIT 50;')
    const calls: (SalesCallRecord & { lines: SalesCallItemRecord[] })[] = []

    while (callStmt.step()) {
      const row = callStmt.getAsObject() as Record<string, unknown>
      const callId = String(row.id)

      const itemStmt = db.prepare('SELECT * FROM sales_call_items WHERE sales_call_id = ?;')
      itemStmt.bind([callId])
      const lines: SalesCallItemRecord[] = []
      while (itemStmt.step()) {
        const itemRow = itemStmt.getAsObject() as Record<string, unknown>
        lines.push({
          id: String(itemRow.id),
          salesCallId: String(itemRow.sales_call_id),
          itemId: String(itemRow.item_id),
          itemCode: String(itemRow.item_code),
          description: String(itemRow.description),
          sellingPrice: Number(itemRow.selling_price || 0),
          orderQty: Number(itemRow.order_qty || 0),
          unitOfMeasure: String(itemRow.unit_of_measure || 'EA'),
          discountPercent: Number(itemRow.discount_percent || 0),
          lineTotal: Number(itemRow.line_total || 0),
        })
      }
      itemStmt.free()

      calls.push({
        id: callId,
        companyId: String(row.company_id),
        salesRepId: String(row.sales_rep_id),
        customerId: String(row.customer_id),
        customerName: String(row.customer_name),
        routeId: row.route_id ? String(row.route_id) : undefined,
        callDate: String(row.call_date),
        startTime: String(row.start_time),
        endTime: row.end_time ? String(row.end_time) : undefined,
        status: String(row.status),
        notes: row.notes ? String(row.notes) : undefined,
        grandTotal: Number(row.grand_total || 0),
        isSynced: Number(row.is_synced || 0),
        syncedAt: row.synced_at ? String(row.synced_at) : undefined,
        serverOrderId: row.server_order_id ? String(row.server_order_id) : undefined,
        serverOrderNumber: row.server_order_number ? String(row.server_order_number) : undefined,
        lines,
      })
    }
    callStmt.free()
    return calls
  }

  public async markCallSynced(
    clientCallId: string,
    serverOrderId?: string,
    serverOrderNumber?: string,
  ): Promise<void> {
    const db = await this.getDb()
    const now = new Date().toISOString()
    db.run(
      `UPDATE sales_calls
       SET is_synced = 1,
           synced_at = ?,
           server_order_id = ?,
           server_order_number = ?
       WHERE id = ?;`,
      [now, serverOrderId || null, serverOrderNumber || null, clientCallId],
    )
    await this.saveToIndexedDb()
  }

  // --- Diagnostics & Database Stats ---
  public async getDatabaseStats(): Promise<DatabaseStats> {
    const db = await this.getDb()

    const getCount = (table: string): number => {
      try {
        const res = db.exec(`SELECT COUNT(*) AS count FROM ${table};`)
        return (res[0]?.values[0]?.[0] as number) || 0
      } catch {
        return 0
      }
    }

    const routesCount = getCount('routes')
    const productsCount = getCount('products')
    const pricingRulesCount = getCount('pricing_rules')
    const salesCallsCount = getCount('sales_calls')

    let unsyncedCallsCount = 0
    try {
      const res = db.exec('SELECT COUNT(*) FROM sales_calls WHERE is_synced = 0;')
      unsyncedCallsCount = (res[0]?.values[0]?.[0] as number) || 0
    } catch {
      unsyncedCallsCount = 0
    }

    let lastSyncTime: string | undefined
    try {
      const res = db.exec("SELECT value FROM meta WHERE key = 'last_sync_time';")
      if (res[0]?.values[0]?.[0]) {
        lastSyncTime = String(res[0].values[0][0])
      }
    } catch {
      // Ignored
    }

    const binary = db.export()
    const databaseSizeKb = Math.round((binary.byteLength / 1024) * 10) / 10

    return {
      routesCount,
      productsCount,
      pricingRulesCount,
      salesCallsCount,
      unsyncedCallsCount,
      lastSyncTime,
      databaseSizeKb,
    }
  }

  /**
   * Load a previously exported SQLite database file (e.g. one downloaded from
   * the login page) and make it the active offline database. Validates that the
   * file is a NERP salesperson database before replacing the current one.
   */
  public async loadDatabaseFromBinary(binary: ArrayBuffer | Uint8Array): Promise<DatabaseStats> {
    await this.getDb() // Ensure the service is initialized before swapping.

    const SQL = await this.getSql()
    const bytes = binary instanceof Uint8Array ? binary : new Uint8Array(binary)

    let incoming: SqliteDatabase
    try {
      incoming = new SQL.Database(bytes)
    } catch {
      throw new Error('The selected file is not a valid SQLite database.')
    }

    let tableNames: string[] = []
    try {
      const res = incoming.exec("SELECT name FROM sqlite_master WHERE type='table';")
      tableNames = res.length ? (res[0]?.values ?? []).map((v) => String(v[0])) : []
    } catch {
      incoming.close()
      throw new Error('The selected file is not a valid SQLite database.')
    }

    if (!tableNames.includes('routes') && !tableNames.includes('products')) {
      incoming.close()
      throw new Error('This file is not a NERP salesperson offline database.')
    }

    if (this.db) {
      try {
        this.db.close()
      } catch {
        // Ignore; the database is being replaced regardless.
      }
    }

    this.db = incoming
    this.initPromise = Promise.resolve(incoming)

    // Bring older exports up to the current schema, then persist.
    this.createSchema(incoming)
    await this.saveToIndexedDb()

    return this.getDatabaseStats()
  }

  /**
   * Export the SQLite binary file as a Blob for download / inspection.
   */
  public async exportDatabaseBlob(): Promise<Blob> {
    const db = await this.getDb()
    const binary = db.export()
    return new Blob([binary.buffer as ArrayBuffer], { type: 'application/x-sqlite3' })
  }
}

export const sqliteService = new SqliteService()
