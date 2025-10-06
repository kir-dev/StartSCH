interface Window {
    beforeSignOut: (event: SubmitEvent) => Promise<void>
    isAuthenticated: boolean
    serviceWorkerFingerprint: string
}
