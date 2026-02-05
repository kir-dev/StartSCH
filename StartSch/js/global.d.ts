interface Window {
    beforeSignOut: (event: SubmitEvent) => Promise<void>
    interestIndexJson: string
    isAuthenticated: boolean
    administeredPageIds: number[]
    
    // Hashes of push endpoints that were registered in the DB when the page loaded
    registeredPushEndpointHashesGlobal: string[] | null
    
    serviceWorkerFingerprint: string
}
