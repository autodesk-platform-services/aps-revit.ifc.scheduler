import {action, makeObservable, observable} from "mobx";
import dayjs from "dayjs";
import advancedFormat from 'dayjs/plugin/advancedFormat';
import {isValidCron} from "cron-validator";
import cronstrue from "cronstrue";

dayjs.extend(advancedFormat);

enum EnumCron {
  "minutes",
  "hours",
  "days",
  "months",
  "daysOfWeek"
}

export class Cron {
  @observable protected _cycle: "hourly" | "daily" | "weekly" | "monthly" | "yearly" | "custom" = 'daily';
  @observable protected _minutes: string[] = ['0'];
  @observable protected _hours: string[] = ['0'];
  @observable protected _days: string[] = ['*'];
  @observable protected _months: string[] = ['*'];
  @observable protected _daysOfWeek: string[] = ['*'];
  @observable protected _customCron?: string;

  // @action
  constructor(cronExpression?: string){
    makeObservable(this);
    if(!cronExpression || !cronExpression.trim()){
      cronExpression = '0 0 * * *'
    }

    const cronSplit = cronExpression.split(' ');
    this._minutes = cronSplit[EnumCron.minutes].split(',').filter(val=>val !== '');
    this._hours = cronSplit[EnumCron.hours].split(',').filter(val=>val !== '');
    this._days = cronSplit[EnumCron.days].split(',').filter(val=>val !== '');
    this._months = cronSplit[EnumCron.months].split(',').filter(val=>val !== '');
    this._daysOfWeek = cronSplit[EnumCron.daysOfWeek].split(',').filter(val=>val !== '');
    this._customCron = cronExpression;

    if(this._minutes.length === 0){
      this._minutes = ['0']
    }

    if(cronExpression.indexOf('/') > -1){
      this._cycle = "custom";
    }else if(
      this.months.join('') !== "*"
      && this.daysOfWeek.join('') === "*")
    {
      this._cycle = "yearly"
    } else if(
      this.months.join('') === "*"
      && this.daysOfWeek.join('') === "*"
      && this.days.join('') !== "*")
    {
      this._cycle = "monthly"
    } else if(
      this.months.join('') === "*"
      && this.days.join('') === "*"
      && this.daysOfWeek.join('') !== "*")
    {
      this._cycle = "weekly"
    } else if(
      this.months.join('') === "*"
      && this.days.join('') === "*"
      && this.daysOfWeek.join('') === "*"
      && this.hours.join('') !== "*")
    {
      this._cycle = "daily"
    } else{
      this._cycle = "hourly"
    }
  }

  public static IsValid(expression: string): boolean{
    return isValidCron(expression);
  }

  public toString(): string{
    const modifiedHours = this.cycle !== 'hourly' && this.hours.join("") === "*" ? ["0"] : this.hours;

    if(this.cycle === 'hourly') {
      return `${this.minutes.join(',')} * * * *`;
    } else if(this.cycle === 'daily'){
      return `${this.minutes.join(',')} ${modifiedHours.join(',')} * * *`;
    } else if(this.cycle === 'weekly'){
      return `${this.minutes.join(',')} ${modifiedHours.join(',')} * * ${this.daysOfWeek.join(',')}`;
    } else if (this.cycle === 'monthly'){
      return `${this.minutes.join(',')} ${modifiedHours.join(',')} ${this.days.join(',')} * *`;
    } else if (this.cycle === 'yearly'){
      return `${this.minutes.join(',')} ${modifiedHours.join(',')} ${this.days.join(',')} ${this.months.join(',')} *`;
    } else if (this.cycle === 'custom' && this._customCron){
      return this._customCron
    } else {
      return `0 0 * * *`;
    }
  }

  @action public setExpression(expression: string){

    const cronSplit = expression.split(' ');
    this._minutes = cronSplit[EnumCron.minutes].split(',').filter(val=>val !== '');
    this._hours = cronSplit[EnumCron.hours].split(',').filter(val=>val !== '');
    this._days = cronSplit[EnumCron.days].split(',').filter(val=>val !== '');
    this._months = cronSplit[EnumCron.months].split(',').filter(val=>val !== '');
    this._daysOfWeek = cronSplit[EnumCron.daysOfWeek].split(',').filter(val=>val !== '');
    this._customCron = expression;
  }

  public description(): string{
    if(isValidCron(this.toString())){
      return cronstrue.toString(this.toString());
    } else {
      return "---"
    }
  }

  @action
  public setCycle(cycle: "hourly" | "daily" | "weekly" | "monthly" | "yearly"): void{
    if(cycle !== this._cycle) {
      this._cycle = cycle;
    }
  }

  get months(){return this._months}
  get cycle(){return this._cycle}
  get days(){return this._days}
  get daysOfWeek(){return this._daysOfWeek}
  get hours(){return this._hours;}
  get minutes(){return this._minutes;}

  @action public setDaysOfMonth(days: string[]){
    let _days: string[];
    if(this._days.indexOf("*") > -1 && days.length > 1){
      _days = days.filter(val=>val !== "*")
    } else if(days.indexOf("*") > -1 || days.length === 0){
      _days = ["*"]
    } else {
      _days = days
    }

    this._days = _days.sort((a,b)=>a.localeCompare(b, 'en', {numeric: true}))
  }

  @action public setDaysOfWeek(daysOfWeek: string[]): void{
    let _daysOfWeek: string[];
    if(this._daysOfWeek.indexOf("*") > -1 && daysOfWeek.length > 1){
      _daysOfWeek = daysOfWeek.filter(val=>val !== "*")
    } else if(daysOfWeek.indexOf("*") > -1 || daysOfWeek.length === 0){
      _daysOfWeek = ["*"]
    } else {
      _daysOfWeek = daysOfWeek
    }

    this._daysOfWeek = _daysOfWeek.sort((a,b)=>a.localeCompare(b, 'en', {numeric: true}))
  }

  @action public setMonths(months: string[]): void{
    let _months: string[];
    if(this._months.indexOf("*") > -1 && months.length > 1){
      _months = months.filter(val=>val !== "*")
    } else if(months.indexOf("*") > -1 || months.length === 0){
      _months = ["*"]
    } else {
      _months = months
    }

    this._months = _months.sort((a,b)=>a.localeCompare(b, 'en', {numeric: true}))
  }

  @action public setHours(hours: string[]): void{
    this._hours = hours;
  }

  @action public setMinutes(minutes: string[]): void{
    const filtered = minutes.filter(val=> +val >= 0 && +val < 60).map(val=>val.slice(-2));
    this._minutes = filtered.length ? filtered : ["0"];
  }

}