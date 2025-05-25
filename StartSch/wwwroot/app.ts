import "./push-notifications";
import "./components/tag";
import "./interest-index";
import {main} from "./interest-index";

declare interface Window {
    interestIndex: InterestIndexDto;
}

interface InterestIndexDto {
    pages: PageDto[]
}

interface PageDto {
    id: number
    name: string
    categories: CategoryDto[]
}

interface CategoryDto {
    id: number
    interests: InterestDto[]
}

interface InterestDto {
    id: number
    name: string
}

document.addEventListener("DOMContentLoaded", () => {
    main()
});
