interface InterestIndexDto {
    pages: PageDto[];
    subscriptions: number[];
}

interface PageDto {
    id: number;
    name: string;
    categories: CategoryDto[];
}

interface CategoryDto {
    id: number;
    interests: InterestDto[];
    includedCategories: number[];
}

interface InterestDto {
    id: number;
    name: string;
}

export interface Page {
    id: number
    name: string
    categories: Category[];
}

export interface Category {
    id: number
    page: Page
    interests: Interest[];
    includedCategories: Category[];
    includerCategories: Category[];
}

export interface Interest {
    id: number
    name: string
}

declare const interestIndexJson: string; // set by blazor

const interestIndexDto = JSON.parse(interestIndexJson) as InterestIndexDto;

const pages = new Map<number, Page>();
const categories = new Map<number, Category>();
const interests = new Map<number, Interest>();
const subscriptions = new Set<number>(interestIndexDto.subscriptions);

const pageDtos = new Map<number, PageDto>();
const categoryDtos = new Map<number, CategoryDto>();
const interestDtos = new Map<number, InterestDto>();
for (const page of interestIndexDto.pages) {
    pageDtos.set(page.id, page);
    for (const category of page.categories) {
        categoryDtos.set(category.id, category);
        for (const interest of category.interests) {
            interestDtos.set(interest.id, interest);
        }
    }
}

for (const pageDto of pageDtos.values()) {
    const page: Page = {
        id: pageDto.id,
        name: pageDto.name,
        categories: []
    };
    pages.set(page.id, page);
    for (const categoryDto of pageDto.categories) {
        const category: Category = {
            id: categoryDto.id,
            page: page,
            interests: [],
            includedCategories: [],
            includerCategories: []
        };
        categories.set(category.id, category);
        page.categories.push(category);
        for (const interestDto of categoryDto.interests) {
            const interest: Interest = {
                id: interestDto.id,
                name: interestDto.name
            };
            interests.set(interest.id, interest);
            category.interests.push(interest);
        }
    }
}

for (const category of categories.values()) {
    const dto = categoryDtos.get(category.id)!;
    for (const includedCategoryId of dto.includedCategories) {
        const includedCategory = categories.get(includedCategoryId)!;
        includedCategory.includerCategories.push(category);
        category.includedCategories.push(includedCategory);
    }
}

export const InterestIndex = {
    pages,
    categories,
    interests,
    subscriptions,
};
