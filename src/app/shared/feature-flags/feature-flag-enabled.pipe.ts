import { Pipe, PipeTransform } from '@angular/core';
import { Observable } from 'rxjs';
import { FeatureFlagService } from './feature-flags.service';

@Pipe({
  name: 'featureFlagEnabled'
})
export class FeatureFlagEnabledPipe implements PipeTransform {
  constructor(private _featureFlagService: FeatureFlagService) { }

  public transform(flagName: string): Observable<boolean> {
    return this._featureFlagService.isFlagEnabled(flagName);
  }
}