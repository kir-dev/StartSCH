import {Dialog} from "@material/web/dialog/internal/dialog";

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
