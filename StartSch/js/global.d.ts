import type {Dialog} from "@material/web/dialog/internal/dialog";
import type {MdChipSet} from "@material/web/chips/chip-set";

declare global {
    interface Window {
        beforeSignOut: (event: SubmitEvent) => Promise<void>
        interestIndexJson: string
        isAuthenticated: boolean
        administeredPageIds: Set<number>

        // Hashes of push endpoints that were registered in the DB when the page loaded
        registeredPushEndpointHashesGlobal: string[] | null

        serviceWorkerFingerprint: string

        showDialog: (dialog: Dialog) => void
        dialogGetReturnValue: (dialog: Dialog) => string
        setElementProperty: (element: HTMLElement, property: string, value: any) => void
        stopSubmitBubbling: (element: HTMLElement) => void
        scrollSelectedChipIntoView: (chipSet: MdChipSet) => boolean
    }
}
