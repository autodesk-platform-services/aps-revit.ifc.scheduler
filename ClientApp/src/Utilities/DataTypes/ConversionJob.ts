import {ISchedule, Schedule} from "./Schedule";
import {Project} from "./Project";
import {computed} from "mobx";
import dayjs, {Dayjs} from "dayjs";
import utc from 'dayjs/plugin/utc';
import timezone from 'dayjs/plugin/timezone';
import calendar from 'dayjs/plugin/calendar';
dayjs.extend(utc);
dayjs.extend(timezone);
dayjs.extend(calendar);

export interface IConversionJob{
    id: string;
    ifcSettingsSetName: string;
    schedule?: ISchedule;
    fileUrn: string;
    projectId: string;
    folderId: string;
    folderUrl: string;
    fileName: string;
    itemId: string;
    jobCreated: string;
    notes?: string;
    jobFinished?: string;
    status: string;
    createdBy: string;
    isCompositeDesign: boolean;
}

export class ConversionJob{
    public readonly id: string;
    public readonly ifcSettingsSetName: string;
    public readonly schedule?: Schedule;
    public readonly projectId: string;
    public readonly folderId: string;
    public readonly folderUrl: string;
    public readonly fileUrn: string;
    public readonly fileName: string;
    public readonly itemId: string;
    public readonly jobCreated: Dayjs;
    public readonly jobFinished?: Dayjs;
    public readonly notes: string;
    public readonly status: string;
    public readonly createdBy: string;
    public readonly isCompositeDesign: boolean;

    constructor(props: IConversionJob, project: Project) {
        this.id = props.id;
        this.ifcSettingsSetName = props.ifcSettingsSetName;
        this.schedule = props.schedule ? new Schedule(props.schedule, project) : undefined;
        this.fileUrn = props.fileUrn;
        this.folderId = props.folderId;
        this.folderUrl = props.folderUrl;
        this.projectId = props.projectId;
        this.fileName = props.fileName;
        this.itemId = props.itemId;
        this.jobCreated = dayjs.utc(props.jobCreated).tz(dayjs.tz.guess());
        this.jobFinished = props.jobFinished ? dayjs.utc(props.jobFinished).tz(dayjs.tz.guess()) : undefined;
        this.notes = props.notes || "";
        this.status = props.status;
        this.createdBy = props.createdBy;
        this.isCompositeDesign = props.isCompositeDesign;
    }

    @computed public get searchTerm(){
        return [
            this.ifcSettingsSetName,
            this.schedule?.name,
            this.fileUrn,
            this.fileName,
            this.itemId,
            this.jobCreated,
            this.jobFinished,
            this.status,
            this.createdBy,
        ]
            .join('')
            .toLowerCase()
    }
}