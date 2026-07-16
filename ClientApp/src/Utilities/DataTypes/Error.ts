import {makeObservable, observable} from "mobx";
import {AppState} from "../AppState";

interface IError {
    title: string;
    message: string;
    appState: AppState;
}

export class Error{

    @observable private readonly _title: string;
    @observable private readonly _message: string;
    private readonly _appState: AppState;

    constructor({title, message, appState}: IError){
        makeObservable(this);
        this._title = title;
        this._message = message;
        this._appState = appState;

        this.clear = this.clear.bind(this);
    }

    static create({title, message, appState}: IError){
        const newError = new Error({title, message, appState});
        appState.setErrors(appState.errors.concat([newError]));
    }

    public clear(){
        this._appState.setErrors(this._appState.errors.filter(val=>val!==this));
    }

    public get title(){return this._title}
    public get message(){return this._message}
}