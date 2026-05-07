import type {Dialog} from "@material/web/dialog/internal/dialog";

// @ts-ignore
window.showDialog = (dialog: Dialog) => {
    dialog.show().then()
}

// @ts-ignore
window.dialogGetReturnValue = (dialog: Dialog) => {
    const returnValue = dialog.returnValue;
    dialog.returnValue = "";
    return returnValue;
}

// @ts-ignore
window.setElementProperty = (element: HTMLElement, property: string, value: any) => {
    (element as any)[property] = value;
}

// @ts-ignore
window.stopSubmitBubbling = (element: HTMLElement) => element.addEventListener("submit", e => e.stopPropagation());
