// localStorage-like API, but based on IndexedDB, so that it is usable both from normal code and service workers
//
// written by claude.ai
const kvStore = {
    async set(key, value) {
        const db = await openDB();
        const tx = db.transaction(['kvStore'], 'readwrite');
        const store = tx.objectStore('kvStore');
        await store.put({ id: String(key), value: String(value) });
        await tx.done;
        db.close();
    },

    /** @returns {string | null} */
    async get(key) {
        const db = await openDB();
        const tx = db.transaction(['kvStore'], 'readonly');
        const store = tx.objectStore('kvStore');
        const item = await store.get(String(key));
        await tx.done;
        db.close();
        return item.result?.value;
    },

    async remove(key) {
        const db = await openDB();
        const tx = db.transaction(['kvStore'], 'readwrite');
        const store = tx.objectStore('kvStore');
        await store.delete(String(key));
        await tx.done;
        db.close();
    },
};

async function openDB() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open('kvStore', 1);

        request.onupgradeneeded = (e) => {
            const db = e.target.result;
            db.createObjectStore('kvStore', { keyPath: 'id' });
        };

        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}

// Add transaction.done promise helper
Object.defineProperty(IDBTransaction.prototype, 'done', {
    get() {
        return new Promise((resolve, reject) => {
            this.oncomplete = () => resolve();
            this.onerror = () => reject(this.error);
        });
    }
});