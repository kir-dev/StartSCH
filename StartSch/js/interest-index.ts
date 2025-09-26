import {SignalMap} from "signal-utils/map";
import {SignalSet} from "signal-utils/set";
import {Signal} from "signal-polyfill";

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
    name?: string;
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
    name?: string,
    page: Page
    interests: Interest[];
    includedCategories: Category[];
    includerCategories: Category[];
}

export interface Interest {
    id: number
    name: string
    category: Category
}

declare const interestIndexJson: string; // set by blazor

const interestIndexDto = JSON.parse(interestIndexJson) as InterestIndexDto;

const pages = new Map<number, Page>();
const categories = new Map<number, Category>();
const interests = new Map<number, Interest>();
const subscriptions = new SignalSet<number>(interestIndexDto.subscriptions);

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
            name: categoryDto.name,
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
                name: interestDto.name,
                category: category,
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

export enum InterestSelectionState {
    Selected,
    IncluderSelected,
    None,
}

export class CategoryState {
    private readonly _category: Category;
    private _selected?: Signal.Computed<boolean>;
    private _includerSelected?: Signal.Computed<boolean>;
    private _includedSelected?: Signal.Computed<boolean>;
    
    constructor(category: Category) {
        this._category = category;
    }
    
    public get selected() {
        return this._selected ??= new Signal.Computed(() => {
            for (const interest of this._category.interests) {
                const interestState = getInterestSelectionState(interest).get();
                if (interestState === InterestSelectionState.Selected)
                    return true;
            }
            return false;
        });
    }
    
    public get includerSelected() {
        return this._includerSelected ??= new Signal.Computed(() => {
            for (const includerCategory of this._category.includerCategories) {
                const includerState = getCategorySelectionState(includerCategory);
                if (includerState.selected || includerState.includerSelected)
                    return true;
            }
            return false;
        })
    }
    
    public get includedSelected() {
        return this._includedSelected ??= new Signal.Computed(() => {
            for (const includedCategory of this._category.includedCategories) {
                const includedState = getCategorySelectionState(includedCategory);
                if (includedState.selected || includedState.includerSelected)
                    return true;
            }
            return false;
        })
    }
}

const interestSelectionStateCache = new Map<Interest, Signal.Computed<InterestSelectionState>>();
const categorySelectionStateCache = new Map<Category, CategoryState>();

function getInterestSelectionState(interest: Interest): Signal.Computed<InterestSelectionState> {
    let signal = interestSelectionStateCache.get(interest);
    
    if (!signal) {
        signal = new Signal.Computed(
            () => {
                if (subscriptions.has(interest.id))
                    return InterestSelectionState.Selected;
                
                for (const includerCategory of interest.category.includerCategories) {
                    const parentInterest = includerCategory.interests.find(i => i.name === interest.name);
                    if (!parentInterest) continue;
                    
                    const parentState = InterestIndex.getInterestSelectionState(parentInterest).get();
                    if (parentState === InterestSelectionState.Selected || parentState === InterestSelectionState.IncluderSelected)
                        return InterestSelectionState.IncluderSelected;
                }
                
                return InterestSelectionState.None;
            }
        );
        interestSelectionStateCache.set(interest, signal);
    }
    
    return signal;
}

function getCategorySelectionState(category: Category): CategoryState {
    let state = categorySelectionStateCache.get(category);
    
    if (!state) {
        state = new CategoryState(category);
        categorySelectionStateCache.set(category, state);
    }
    
    return state;
}

export const InterestIndex = {
    pages,
    categories,
    interests,
    subscriptions,
    getCategorySelectionState,
    getInterestSelectionState,
};
