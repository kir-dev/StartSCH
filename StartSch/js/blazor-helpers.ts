import {Dialog} from "@material/web/dialog/internal/dialog";

// @ts-ignore
window.showDialog = (dialog: Dialog) => {
    dialog.show().then()
}

// @ts-ignore
window.closeDialog = (dialog: Dialog) => {
    dialog.close().then();
}

// @ts-ignore
window.dialogGetReturnValue = (dialog: Dialog) => {
    return dialog.returnValue;
}
