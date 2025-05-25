declare namespace startSch {
    let interestIndexJson: string;
}

interface InterestDto {
    id: number;
    name: string;
}

interface CategoryDto {
    id: number;
    interests: InterestDto[];
    includedCategories: number[];
}

interface PageDto {
    id: number;
    name: string;
    categories: CategoryDto[];
}

interface InterestIndexDto {
    pages: PageDto[];
}

function parseInterestIndex(): InterestIndexDto {
    return JSON.parse(startSch.interestIndexJson) as InterestIndexDto;
}

export function main() {
    const interestIndexData = parseInterestIndex();
    if (interestIndexData) {
        console.log(interestIndexData.pages);
    }
}
