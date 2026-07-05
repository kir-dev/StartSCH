import type {Dialog} from "@material/web/dialog/internal/dialog";
import type {MdChipSet} from "@material/web/chips/chip-set";
import type {MdFilterChip} from "@material/web/chips/filter-chip";

window.showDialog = (dialog: Dialog) => {
    dialog.show().then()
}

window.dialogGetReturnValue = (dialog: Dialog) => {
    const returnValue = dialog.returnValue;
    dialog.returnValue = "";
    return returnValue;
}

window.setElementProperty = (element: HTMLElement, property: string, value: any) => {
    (element as any)[property] = value;
}

window.stopSubmitBubbling = (element: HTMLElement) => element.addEventListener("submit", e => e.stopPropagation());

window.scrollSelectedChipIntoView = (chipSet?: MdChipSet) => {
    if (!chipSet || !chipSet.children)
        return false;
    const selected: MdFilterChip[] = [];
    for (const chipElement of chipSet.children) {
        const chip = chipElement as MdFilterChip;
        if (chip.selected)
            selected.push(chip);
    }
    if (selected.length !== 1)
        return false;
    setTimeout(() => {
        selected[0].scrollIntoView({behavior: "smooth", block: "center", inline: "center"});
    }, 10)
    return true;
};
