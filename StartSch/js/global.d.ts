import {Dialog} from "@material/web/dialog/internal/dialog";

interface Window {
    beforeSignOut: (event: SubmitEvent) => Promise<void>
    closeDialog: (dialog: Dialog) => void
    interestIndexJson: string
    isAuthenticated: boolean
    administeredPageIds: Set<number>
    
    // Hashes of push endpoints that were registered in the DB when the page loaded
    registeredPushEndpointHashesGlobal: string[] | null
    
    serviceWorkerFingerprint: string
}
