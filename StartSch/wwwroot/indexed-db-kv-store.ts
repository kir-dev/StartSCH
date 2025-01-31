// localStorage-like API, but based on IndexedDB, so that it is usable both from normal code and service workers
//
// written by claude.ai

async function done(tx: IDBTransaction){
    return new Promise<void>((resolve, reject) => {
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
    });
}

export async function set(key: string, value: string) {
    const db = await openDB();
    const tx = db.transaction(['kvStore'], 'readwrite');
    const store = tx.objectStore('kvStore');
    store.put({ id: key, value: value });
    await done(tx);
    db.close();
}

export async function get(key: string): Promise<string | null> {
    const db = await openDB();
    const tx = db.transaction(['kvStore'], 'readonly');
    const store = tx.objectStore('kvStore');
    const item = store.get(String(key));
    await done(tx);
    db.close();
    return item.result?.value;
}

export async function remove(key: string) {
    const db = await openDB();
    const tx = db.transaction(['kvStore'], 'readwrite');
    const store = tx.objectStore('kvStore');
    store.delete(String(key));
    await done(tx);
    db.close();
}

async function openDB() {
    return new Promise<IDBDatabase>((resolve, reject) => {
        const request = indexedDB.open('kvStore', 1);

        request.onupgradeneeded = _ => {
            const db = request.result;
            db.createObjectStore('kvStore', { keyPath: 'id' });
        };

        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}
